using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.EnterpriseServices;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web;
using System.Web.Hosting;

namespace DAL
{
    ///////////////////////////////////////////////////////////////
    // Ce patron de classe permet de stocker dans un fichier JSON
    // une collection d'objects. Ces derniers doivent posséder
    // la propriété int Id {get; set;}
    // Après l'instanciation il faut invoquer la méthode Init
    // pour fournir le chemin d'accès du fichier JSON.
    // 
    // Tous les membres annotés avec [asset(folder, defaultValue)] 
    // seront traités en tant que données hors BD
    //
    // Auteur : Nicolas Chourot
    // date: Janvier 2025
    ///////////////////////////////////////////////////////////////
    public class Repository<T>
    {
        #region "Méthodes et propritées privées"
        // Pour indiquer si une transaction est en cours
        static bool TransactionOnGoing = false;
        // Pour la gestion d'imbrications de transactions
        static int NestedTransactionsCount = 0;
        // utilisé pour prévenir des conflits entre processus
        static private readonly Mutex mutex = new Mutex();
        // cache des données du fichier JSON
        private List<T> dataList;
        // chemin d'accès absolue du fichier JSON
        private string FilePath;
        // Numéro de serie des données
        private string _SerialNumber;
        // Gestion des images hors données
        private readonly ImageAsset<T> ImageAsset = new ImageAsset<T>();
        // retourne la valeur de l'attribut attributeName de l'intance data de classe T
        private object GetAttributeValue(T data, string attributeName)
        {
            return data.GetType().GetProperty(attributeName).GetValue(data, null);
        }
        // affecter la valeur de l'attribut attributeName de l'intance data de classe T
        private void SetAttributeValue(T data, string attributeName, object value)
        {
            data.GetType().GetProperty(attributeName).SetValue(data, value, null);
        }
        // Vérifier si l'attribut attributeName est présent dans la classe T
        private bool AttributeNameExist(string attributeName)
        {
            var instance = Activator.CreateInstance(typeof(T));
            var type = instance.GetType();
            var pro = type.GetProperty(attributeName);
            return (instance.GetType().GetProperty(attributeName) != null);
        }
        // retourne la valeur de l'attribut Id d'une instance de classe T
        private int Id(T data)
        {
            return (int)GetAttributeValue(data, "Id");
        }
        // Lecture du fichier JSON et conservation des données dans la cache dataList
        private void ReadFile()
        {
            MarkHasChanged();
            dataList.Clear();
            try
            {
                using (var sr = new StreamReader(FilePath))
                    dataList = JsonConvert.DeserializeObject<List<T>>(sr.ReadToEnd());
            }
            catch (Exception e)
            {
                throw e;
            }
            if (dataList == null)
                dataList = new List<T>();
        }
        // Mise à jour du fichier JSON avec les données présentes dans la cache dataList
        private void UpdateFile()
        {
            try
            {
                using (var sw = new StreamWriter(FilePath))
                    sw.WriteLine(JsonConvert.SerializeObject(dataList));
                ReadFile();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        // retourne le prochain Id disponible
        private int NextId()
        {
            int maxId = 0;
            foreach (var data in dataList)
            {
                int Id = this.Id(data);
                if (Id > maxId) maxId = Id;
            }
            return maxId + 1;
        }
        #endregion

        #region "Méthodes publiques"
        // constructeur
        public Repository()
        {
            dataList = new List<T>();
            try
            {
                // s'assurer que la propriété int Id {get; set;} est belle et bien dans la classe T
                var idExist = AttributeNameExist("Id");
                if (!idExist)
                    throw new Exception("The class Repository cannot work with types that does not contain an attribute named Id of type int.");
                string serverPath = HostingEnvironment.MapPath(@"~/App_Data/");
                FilePath = Path.Combine(serverPath, typeof(T).Name + "s.json");
                Init(FilePath);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public bool HasChanged
        {
            get
            {
                if (IsMarkedChanged)
                {
                    HttpContext.Current.Session[this.GetType().Name] = _SerialNumber;
                    return true;
                }
                return false;
            }
        }
        public bool IsMarkedChanged
        {
            get { return ((string)HttpContext.Current.Session[this.GetType().Name] != _SerialNumber); }
        }
        public void BeginTransaction()
        {
            if (!TransactionOnGoing) // todo check if nested transactions still work
            {
                mutex.WaitOne();
                TransactionOnGoing = true;
            }
            else
            {
                NestedTransactionsCount++;
            }
        }
        public void EndTransaction()
        {
            if (NestedTransactionsCount <= 0)
            {
                TransactionOnGoing = false;
                mutex.ReleaseMutex();
            }
            else
            {
                if (NestedTransactionsCount > 0)
                    NestedTransactionsCount--;
            }
        }
        // Init : reçoit le chemin d'accès absolue du fichier JSON
        // Cette méthode doit avoir été appelée avant tout
        public virtual void Init(string filePath)
        {
            if (!TransactionOnGoing) mutex.WaitOne();
            try
            {
                FilePath = filePath;
                if (string.IsNullOrEmpty(FilePath))
                {
                    throw new Exception("FilePath not set exception");
                }
                if (!File.Exists(FilePath))
                {
                    using (StreamWriter sw = File.CreateText(FilePath)) { }
                }
                ReadFile();
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (!TransactionOnGoing) mutex.ReleaseMutex();
            }
        }
        public virtual void MarkHasChanged()
        {
            _SerialNumber = Guid.NewGuid().ToString();
        }

        // Méthodes CRUD

        // Read all
        public List<T> ToList() => dataList;
        // Read one
        public T Get(int Id) => dataList.Where(d => this.Id(d) == Id).FirstOrDefault();
        // Create one
        public virtual int Add(T data)
        {
            int newId = 0;
            if (!TransactionOnGoing) mutex.WaitOne(); // attendre la conclusion d'un appel concurrant
            try
            {
                newId = NextId();
                SetAttributeValue(data, "Id", newId);
                ImageAsset.Update(data);
                dataList.Add(data);
                UpdateFile();
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (!TransactionOnGoing) mutex.ReleaseMutex();
            }
            return newId;
        }
        // Update one
        public virtual bool Update(T data)
        {
            if (!TransactionOnGoing) mutex.WaitOne();
            try
            {
                T dataToUpdate = Get(Id(data));
                if (dataToUpdate != null)
                {
                    ImageAsset.Update(data);
                    dataList[dataList.IndexOf(dataToUpdate)] = data;
                    UpdateFile();
                    return true;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (!TransactionOnGoing) mutex.ReleaseMutex();
            }
            return false;
        }
        // Delete one
        public virtual bool Delete(int Id)
        {
            if (!TransactionOnGoing) mutex.WaitOne();
            try
            {
                T dataToDelete = Get(Id);
                if (dataToDelete != null)
                {
                    ImageAsset.Delete(dataToDelete);
                    dataList.RemoveAt(dataList.IndexOf(dataToDelete));
                    UpdateFile();
                    return true;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (!TransactionOnGoing) mutex.ReleaseMutex();
            }
            return false;
        }

        #endregion
    }
}