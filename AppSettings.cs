using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HelloWorld
{
    /// <summary>
    /// Classe centrale pour la gestion des paramètres de l'application.
    /// Implémente le pattern Singleton pour fournir un accès global aux paramètres.
    /// Coordonne les différents modules de paramètres et gère leur cycle de vie.
    /// </summary>
    public class AppSettings
    {
        #region Singleton

        // Instance unique de la classe
        private static AppSettings _instance;

        // Verrou pour l'initialisation thread-safe du singleton
        private static readonly object _lockObject = new object();

        // Champ privé pour suivre l'état de notification
        private bool _isNotifying = false;

        /// <summary>
        /// Obtient l'instance unique de AppSettings (Singleton).
        /// L'instance est créée lors du premier accès et chargée avec les paramètres par défaut.
        /// </summary>
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new AppSettings();
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Événements

        /// <summary>
        /// Événement déclenché lorsqu'un module de paramètres est modifié.
        /// Fournit des informations sur le module modifié.
        /// </summary>
        public event EventHandler<SettingsModuleChangedEventArgs> ModuleChanged;

        /// <summary>
        /// Arguments pour l'événement ModuleChanged
        /// </summary>
        public class SettingsModuleChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Nom du module qui a été modifié
            /// </summary>
            public string ModuleName { get; }

            /// <summary>
            /// Indique si la modification a été causée par un chargement initial
            /// </summary>
            public bool IsInitialLoad { get; }

            /// <summary>
            /// Crée une nouvelle instance de SettingsModuleChangedEventArgs
            /// </summary>
            /// <param name="moduleName">Nom du module modifié</param>
            /// <param name="isInitialLoad">Indique si c'est un chargement initial</param>
            public SettingsModuleChangedEventArgs(string moduleName, bool isInitialLoad = false)
            {
                ModuleName = moduleName;
                IsInitialLoad = isInitialLoad;
            }
        }

        #endregion

        #region Modules de paramètres

        // Dictionnaire des modules de paramètres
        private readonly Dictionary<string, ISettingsModule> _modules = new Dictionary<string, ISettingsModule>();

        // Module de paramètres du bouton STOP
        private StopButtonSettingsModule _stopButtonSettings;

        /// <summary>
        /// Obtient le module de paramètres du bouton STOP
        /// </summary>
        public StopButtonSettingsModule StopButtonSettings
        {
            get
            {
                if (_stopButtonSettings == null)
                {
                    lock (_lockObject)
                    {
                        if (_stopButtonSettings == null)
                        {
                            _stopButtonSettings = new StopButtonSettingsModule();
                            RegisterModule(_stopButtonSettings);
                        }
                    }
                }
                return _stopButtonSettings;
            }
        }

        // Module de paramètres d'interface
        private InterfaceSettingsModule _interfaceSettings;

        /// <summary>
        /// Obtient le module de paramètres d'interface
        /// </summary>
        public InterfaceSettingsModule InterfaceSettings
        {
            get
            {
                if (_interfaceSettings == null)
                {
                    lock (_lockObject)
                    {
                        if (_interfaceSettings == null)
                        {
                            _interfaceSettings = new InterfaceSettingsModule();
                            RegisterModule(_interfaceSettings);
                        }
                    }
                }
                return _interfaceSettings;
            }
        }

        #endregion

        #region Constructeur et initialisation

        /// <summary>
        /// Constructeur privé pour le pattern Singleton.
        /// Initialise les modules de paramètres et charge les données depuis les fichiers.
        /// </summary>
        private AppSettings()
        {
            // Initialiser les modules de paramètres
            InitializeModules();

            // Créer le répertoire des paramètres s'il n'existe pas
            EnsureSettingsDirectoryExists();

            // Charger les paramètres depuis les fichiers
            LoadAllModules();
        }

        /// <summary>
        /// Initialise tous les modules de paramètres
        /// </summary>
        private void InitializeModules()
        {
            // Initialiser le module de paramètres du bouton STOP
            _stopButtonSettings = new StopButtonSettingsModule();
            RegisterModule(_stopButtonSettings);

            // Initialiser le module de paramètres d'interface
            _interfaceSettings = new InterfaceSettingsModule();
            RegisterModule(_interfaceSettings);
        }

        /// <summary>
        /// S'assure que le répertoire pour stocker les paramètres existe
        /// </summary>
        private void EnsureSettingsDirectoryExists()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string settingsDir = Path.Combine(appDataPath, "HelloWorld");

                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }
            }
            catch (Exception ex)
            {
                JsonSettingsManager.LogError("AppSettings", "Erreur lors de la création du répertoire des paramètres", ex);
            }
        }

        #endregion

        #region Gestion des modules

        /// <summary>
        /// Enregistre un module de paramètres auprès du gestionnaire central
        /// </summary>
        /// <param name="module">Module à enregistrer</param>
        private void RegisterModule(ISettingsModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module), "Le module ne peut pas être null");

            string moduleName = module.ModuleName;

            if (string.IsNullOrEmpty(moduleName))
                throw new ArgumentException("Le nom du module ne peut pas être vide", nameof(module));

            if (_modules.ContainsKey(moduleName))
                throw new InvalidOperationException($"Un module portant le nom '{moduleName}' est déjà enregistré");

            // Ajouter le module au dictionnaire
            _modules[moduleName] = module;

            // S'abonner à l'événement de changement du module
            module.SettingsChanged += Module_SettingsChanged;
        }

        /// <summary>
        /// Désenregistre un module de paramètres
        /// </summary>
        /// <param name="moduleName">Nom du module à désenregistrer</param>
        /// <returns>True si le module a été désenregistré, sinon False</returns>
        private bool UnregisterModule(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
                return false;

            if (_modules.TryGetValue(moduleName, out var module))
            {
                // Se désabonner de l'événement
                module.SettingsChanged -= Module_SettingsChanged;

                // Supprimer du dictionnaire
                _modules.Remove(moduleName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gestionnaire pour l'événement SettingsChanged des modules
        /// </summary>
        private void Module_SettingsChanged(object sender, EventArgs e)
        {
            if (sender is ISettingsModule module)
            {
                // Ajouter une protection contre la récursion infinie
                if (_isNotifying)
                    return;

                try
                {
                    _isNotifying = true;
                    // Déclencher l'événement ModuleChanged
                    ModuleChanged?.Invoke(this, new SettingsModuleChangedEventArgs(module.ModuleName));
                }
                finally
                {
                    _isNotifying = false;
                }
            }
        }

        /// <summary>
        /// Obtient un module de paramètres par son nom
        /// </summary>
        /// <param name="moduleName">Nom du module à récupérer</param>
        /// <returns>Le module demandé ou null s'il n'existe pas</returns>
        public ISettingsModule GetModule(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
                return null;

            if (_modules.TryGetValue(moduleName, out var module))
                return module;

            return null;
        }

        /// <summary>
        /// Obtient la liste des noms de tous les modules enregistrés
        /// </summary>
        /// <returns>Liste des noms de modules</returns>
        public IEnumerable<string> GetModuleNames()
        {
            return _modules.Keys.ToList();
        }

        #endregion

        #region Chargement et sauvegarde

        /// <summary>
        /// Charge tous les modules de paramètres depuis leurs fichiers respectifs
        /// </summary>
        /// <returns>True si tous les modules ont été chargés avec succès, sinon False</returns>
        public bool LoadAllModules()
        {
            bool allSuccess = true;

            foreach (var module in _modules.Values)
            {
                try
                {
                    bool success = module.Load();
                    if (!success)
                    {
                        JsonSettingsManager.LogError("AppSettings", $"Échec du chargement du module '{module.ModuleName}'");
                        allSuccess = false;
                    }
                    else
                    {
                        // Notifier du chargement initial
                        ModuleChanged?.Invoke(this, new SettingsModuleChangedEventArgs(module.ModuleName, true));
                    }
                }
                catch (Exception ex)
                {
                    JsonSettingsManager.LogError("AppSettings", $"Exception lors du chargement du module '{module.ModuleName}'", ex);
                    allSuccess = false;
                }
            }

            return allSuccess;
        }

        /// <summary>
        /// Sauvegarde tous les modules de paramètres dans leurs fichiers respectifs
        /// </summary>
        /// <returns>True si tous les modules ont été sauvegardés avec succès, sinon False</returns>
        public bool SaveAllModules()
        /// N'est finalement pas utilisé cr chaque onglet a son bouton "Appliqué" respectif
        {
            bool allSuccess = true;

            foreach (var module in _modules.Values)
            {
                try
                {
                    bool success = module.Save();
                    if (!success)
                    {
                        JsonSettingsManager.LogError("AppSettings", $"Échec de la sauvegarde du module '{module.ModuleName}'");
                        allSuccess = false;
                    }
                }
                catch (Exception ex)
                {
                    JsonSettingsManager.LogError("AppSettings", $"Exception lors de la sauvegarde du module '{module.ModuleName}'", ex);
                    allSuccess = false;
                }
            }

            return allSuccess;
        }

        /// <summary>
        /// Réinitialise tous les modules de paramètres à leurs valeurs par défaut
        /// </summary>
        public void ResetAllModules()
        {
            foreach (var module in _modules.Values)
            {
                try
                {
                    module.Reset();
                }
                catch (Exception ex)
                {
                    JsonSettingsManager.LogError("AppSettings", $"Exception lors de la réinitialisation du module '{module.ModuleName}'", ex);
                }
            }
        }

        #endregion

        #region Nettoyage des ressources

        /// <summary>
        /// Libère les ressources utilisées par les modules de paramètres
        /// </summary>
        public void Cleanup()
        {
            // Sauvegarder tous les modules avant de quitter
            // SaveAllModules();

            // Désenregistrer tous les modules
            foreach (var moduleName in _modules.Keys.ToList())
            {
                UnregisterModule(moduleName);
            }

            // Vider les références
            _stopButtonSettings = null;
            _interfaceSettings = null;
        }

        #endregion
    }
}