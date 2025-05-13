using System;
using System.ComponentModel;
using System.IO;
using HelloWorld.Preview;

namespace HelloWorld
{
    /// <summary>
    /// Module de paramètres pour l'interface utilisateur de l'application.
    /// Cette classe est une ébauche qui sera complétée ultérieurement.
    /// Elle gère les paramètres actuellement codés en dur dans MainWindow.
    /// </summary>
    public partial class InterfaceSettingsModule : ISettingsModule, INotifyPropertyChanged
    {
        #region Constantes

        // Version actuelle du format de données
        private const int CURRENT_FORMAT_VERSION = 1;

        #endregion

        #region ISettingsModule - Implémentation

        /// <summary>
        /// Obtient le nom du module de paramètres
        /// </summary>
        public string ModuleName => "InterfaceSettings";

        /// <summary>
        /// Obtient une description du module de paramètres
        /// </summary>
        public string Description => "Paramètres généraux de l'interface utilisateur de l'application";

        /// <summary>
        /// Obtient la version actuelle du format de données
        /// </summary>
        public int CurrentFormatVersion => CURRENT_FORMAT_VERSION;

        /// <summary>
        /// Obtient le chemin par défaut pour le fichier de paramètres
        /// </summary>
        public string DefaultFilePath
        {
            get
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appDataPath, "HelloWorld", "InterfaceSettings.json");
            }
        }

        /// <summary>
        /// Événement déclenché lorsque les paramètres sont modifiés
        /// </summary>
        public event EventHandler SettingsChanged;

        /// <summary>
        /// Charge les paramètres depuis un fichier
        /// </summary>
        /// <param name="filePath">Chemin du fichier, ou null pour utiliser le chemin par défaut</param>
        /// <returns>True si le chargement a réussi</returns>
        public bool Load(string filePath = null)
        {
            // Utiliser le chemin par défaut si aucun n'est spécifié
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = DefaultFilePath;
            }

            // Définir une fonction pour créer des paramètres par défaut
            Func<InterfaceSettingsData> createDefault = () => new InterfaceSettingsData();

            // Charger les paramètres depuis le fichier
            bool success;
            var loadedSettings = JsonSettingsManager.LoadFromFile<InterfaceSettingsData>(
                filePath, createDefault, CURRENT_FORMAT_VERSION, out success);

            if (success && loadedSettings != null)
            {
                // Appliquer les paramètres chargés
                UpdateFromData(loadedSettings);

                // Charger les données "À propos" via la méthode déplacée dans la partie About
                UpdateAboutDataFromSettings(loadedSettings);

                // Appliquer immédiatement les paramètres à BaseInfoCollector
                ApplyToBaseInfoCollector();

                return true;
            }

            // Si le chargement a échoué, créer des paramètres par défaut
            Reset();
            return false;
        }

        /// <summary>
        /// Sauvegarde les paramètres dans un fichier
        /// </summary>
        /// <param name="filePath">Chemin du fichier, ou null pour utiliser le chemin par défaut</param>
        /// <returns>True si la sauvegarde a réussi</returns>
        public bool Save(string filePath = null)
        {
            // Utiliser le chemin par défaut si aucun n'est spécifié
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = DefaultFilePath;
            }

            // Créer un objet de données à partir des propriétés actuelles
            var settingsData = CreateDataFromProperties();

            // Sauvegarder les données dans le fichier
            return JsonSettingsManager.SaveToFile(settingsData, filePath, CURRENT_FORMAT_VERSION);
        }

        /// <summary>
        /// Réinitialise les paramètres à leurs valeurs par défaut
        /// </summary>
        public void Reset()
        {
            // Créer un nouvel objet avec les valeurs par défaut
            var defaultSettings = new InterfaceSettingsData();

            // Appliquer les valeurs par défaut
            UpdateFromData(defaultSettings);

            // Réinitialiser les propriétés "À propos" en utilisant la méthode auxiliaire
            ResetAboutData(defaultSettings);

            // Notifier du changement
            NotifySettingsChanged();
        }

        /// <summary>
        /// Méthode auxiliaire pour réinitialiser les données "À propos"
        /// </summary>
        /// <param name="defaultSettings">Paramètres par défaut</param>
        private void ResetAboutData(InterfaceSettingsData defaultSettings)
        {
            // Cette méthode est implémentée dans InterfaceSettingsModule.About.cs
            // Nous déclarons un bloc vide ici pour indiquer que l'implémentation 
            // se trouve dans le fichier partiel

            // Initialiser les données "À propos"
            defaultSettings.InitializeAboutData();
            AboutData = defaultSettings.AboutData;

            // Mettre à jour les propriétés liées au chemin du fichier
            UseCustomAboutFilePath = defaultSettings.UseCustomAboutFilePath;
            CustomAboutFilePath = defaultSettings.CustomAboutFilePath;

            // Notification effectuée dans la méthode Reset() principale
        }

        /// <summary>
        /// Restaure les paramètres à partir du fichier de sauvegarde
        /// </summary>
        /// <returns>True si la restauration a réussi</returns>
        public bool RestoreFromBackup()
        {
            return JsonSettingsManager.RestoreFromBackup<InterfaceSettingsData>(
                DefaultFilePath, () => new InterfaceSettingsData(), CURRENT_FORMAT_VERSION);
        }

        /// <summary>
        /// Crée une copie profonde du module de paramètres
        /// </summary>
        /// <returns>Une copie indépendante du module</returns>
        public ISettingsModule Clone()
        {
            var clone = new InterfaceSettingsModule();

            // Copier toutes les propriétés
            clone.ShowProgressWindow = this.ShowProgressWindow;
            clone.StepDelayInMilliseconds = this.StepDelayInMilliseconds;
            clone.WindowWidth = this.WindowWidth;
            clone.WindowHeight = this.WindowHeight;
            clone.LivePreviewEnabled = this.LivePreviewEnabled;

            // Copier les propriétés "À propos"
            clone.UseCustomAboutFilePath = this.UseCustomAboutFilePath;
            clone.CustomAboutFilePath = this.CustomAboutFilePath;

            // Copier les propriétés de prévisualisation (nouveau)
            clone.IndicatorsType = this.IndicatorsType;
            clone.PreviewMode = this.PreviewMode;
            clone.TemporaryPreviewDuration = this.TemporaryPreviewDuration;
            clone.ShowSnapZones = this.ShowSnapZones;
            clone.PreviewRendererType = this.PreviewRendererType;
            clone.PositionStrategyType = this.PositionStrategyType;

            // Note: Les données AboutData elles-mêmes ne sont pas clonées
            // car elles sont rechargées dynamiquement depuis le fichier

            return clone;
        }

        #endregion

        #region INotifyPropertyChanged - Implémentation

        /// <summary>
        /// Événement déclenché lorsqu'une propriété change
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Méthode pour notifier qu'une propriété a changé
        /// </summary>
        /// <param name="propertyName">Nom de la propriété qui a changé</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Paramètres d'interface

        private bool _showProgressWindow = true;
        /// <summary>
        /// Indique si la fenêtre de progression doit être affichée pendant la collecte
        /// </summary>
        public bool ShowProgressWindow
        {
            get { return _showProgressWindow; }
            set
            {
                if (_showProgressWindow != value)
                {
                    _showProgressWindow = value;
                    OnPropertyChanged(nameof(ShowProgressWindow));
                    NotifySettingsChanged();
                }
            }
        }

        private int _stepDelayInMilliseconds = 500;
        /// <summary>
        /// Délai en millisecondes entre chaque étape de collecte
        /// </summary>
        public int StepDelayInMilliseconds
        {
            get { return _stepDelayInMilliseconds; }
            set
            {
                if (_stepDelayInMilliseconds != value)
                {
                    _stepDelayInMilliseconds = value;
                    OnPropertyChanged(nameof(StepDelayInMilliseconds));
                    NotifySettingsChanged();
                }
            }
        }

        private double _windowWidth = 500;
        /// <summary>
        /// Largeur de la fenêtre principale en pixels
        /// </summary>
        public double WindowWidth
        {
            get { return _windowWidth; }
            set
            {
                if (_windowWidth != value)
                {
                    _windowWidth = value;
                    OnPropertyChanged(nameof(WindowWidth));
                    NotifySettingsChanged();
                }
            }
        }

        private double _windowHeight = 460;
        /// <summary>
        /// Hauteur de la fenêtre principale en pixels
        /// </summary>
        public double WindowHeight
        {
            get { return _windowHeight; }
            set
            {
                if (_windowHeight != value)
                {
                    _windowHeight = value;
                    OnPropertyChanged(nameof(WindowHeight));
                    NotifySettingsChanged();
                }
            }
        }

        private bool _livePreviewEnabled = false;
        /// <summary>
        /// Indique si l'aperçu en direct des changements de dimensions est activé
        /// </summary>
        public bool LivePreviewEnabled
        {
            get { return _livePreviewEnabled; }
            set
            {
                if (_livePreviewEnabled != value)
                {
                    _livePreviewEnabled = value;
                    OnPropertyChanged(nameof(LivePreviewEnabled));
                    NotifySettingsChanged();
                }
            }
        }

        // TODO: Ajouter d'autres paramètres d'interface ici au besoin

        #endregion

        #region Constructeur

        /// <summary>
        /// Crée une nouvelle instance de InterfaceSettingsModule
        /// </summary>
        public InterfaceSettingsModule()
        {
            // Initialiser les données "À propos" en appelant la méthode d'initialisation
            // qui est maintenant dans la partie About de la classe
            InitializeAboutData();
        }

        /// <summary>
        /// Méthode d'initialisation des données "À propos"
        /// Implémentée dans InterfaceSettingsModule.About.cs
        /// </summary>
        private void InitializeAboutData()
        {
            // Cette méthode est implémentée dans InterfaceSettingsModule.About.cs
            var tempData = new InterfaceSettingsData();
            tempData.InitializeAboutData();
            AboutData = tempData.AboutData;
        }

        #endregion

        #region Méthodes utilitaires

        /// <summary>
        /// Notifie que les paramètres ont été modifiés
        /// </summary>
        private void NotifySettingsChanged()
        {
            // Déclencher l'événement SettingsChanged
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Version mise à jour de la méthode UpdateFromData pour inclure les données de prévisualisation
        /// Cette méthode remplace celle définie dans InterfaceSettingsModule.cs
        /// </summary>
        /// <param name="data">Données à appliquer</param>
        private void UpdateFromData(InterfaceSettingsData data)
        {
            // Paramètres de collecte
            ShowProgressWindow = data.ShowProgressWindow;
            StepDelayInMilliseconds = data.StepDelayInMilliseconds;

            // Paramètres de fenêtre
            WindowWidth = data.WindowWidth;
            WindowHeight = data.WindowHeight;
            LivePreviewEnabled = data.LivePreviewEnabled;

            // Paramètres de prévisualisation
            UpdatePreviewDataFromSettings(data);

            // Paramètres "À propos"
            UpdateAboutDataFromSettings(data);

            // TODO: Ajouter d'autres paramètres ici au besoin
        }

        /// <summary>
        /// Version mise à jour de la méthode CreateDataFromProperties pour inclure les données de prévisualisation
        /// Cette méthode remplace celle définie dans InterfaceSettingsModule.cs
        /// </summary>
        /// <returns>Objet de données représentant l'état actuel</returns>
        private InterfaceSettingsData CreateDataFromProperties()
        {
            var data = new InterfaceSettingsData
            {
                // Paramètres de collecte
                ShowProgressWindow = ShowProgressWindow,
                StepDelayInMilliseconds = StepDelayInMilliseconds,

                // Paramètres de fenêtre
                WindowWidth = WindowWidth,
                WindowHeight = WindowHeight,
                LivePreviewEnabled = LivePreviewEnabled,
            };

            // Ajouter les données de prévisualisation
            AddPreviewDataToSettings(data);

            // Ajouter les données "À propos"
            AddAboutDataToSettings(data);

            return data;
        }

        #endregion

        #region Méthodes spécifiques à InterfaceSettingsModule

        /// <summary>
        /// Applique les paramètres à l'objet BaseInfoCollector
        /// </summary>
        public void ApplyToBaseInfoCollector()
        {
            // Les paramètres de BaseInfoCollector sont statiques, donc nous les mettons à jour directement
            // Cette méthode est cruciale car c'est elle qui transfère les paramètres de configuration
            // vers l'objet qui gère réellement les opérations

            // Assurons-nous que les valeurs sont correctement transmises
            BaseInfoCollector.ShowProgressWindow = this.ShowProgressWindow;
            BaseInfoCollector.StepDelayInMilliseconds = this.StepDelayInMilliseconds;

            // Notifier un changement
            NotifySettingsChanged();
        }

        /// <summary>
        /// Charge les paramètres depuis l'objet BaseInfoCollector
        /// </summary>
        public void LoadFromBaseInfoCollector()
        {
            // Synchronise nos propriétés avec les valeurs actuelles de BaseInfoCollector
            ShowProgressWindow = BaseInfoCollector.ShowProgressWindow;
            StepDelayInMilliseconds = BaseInfoCollector.StepDelayInMilliseconds;
        }

        /// <summary>
        /// Applique les paramètres de fenêtre à une fenêtre
        /// </summary>
        /// <param name="window">Fenêtre à laquelle appliquer les paramètres</param>
        /// <param name="forceRestore">Indique si la fenêtre doit être forcée à l'état normal (non maximisée)</param>
        public void ApplyWindowDimensions(System.Windows.Window window, bool forceRestore = false)
        {
            if (window != null)
            {
                // Vérifier si la fenêtre est maximisée
                bool isMaximized = (window.WindowState == System.Windows.WindowState.Maximized);

                // Si la fenêtre est maximisée et qu'on force la restauration
                if (isMaximized && forceRestore)
                {
                    // Restaurer d'abord la fenêtre à l'état normal
                    window.WindowState = System.Windows.WindowState.Normal;
                }

                // Appliquer les dimensions si la fenêtre n'est pas maximisée
                if (window.WindowState != System.Windows.WindowState.Maximized)
                {
                    window.Width = WindowWidth;
                    window.Height = WindowHeight;
                }
            }
        }

        /// <summary>
        /// Charge les dimensions depuis une fenêtre
        /// </summary>
        /// <param name="window">Fenêtre depuis laquelle charger les dimensions</param>
        public void LoadWindowDimensions(System.Windows.Window window)
        {
            if (window != null)
            {
                WindowWidth = window.Width;
                WindowHeight = window.Height;
            }
        }

        #endregion
    }

    /// <summary>
    /// Classe de données pour la sérialisation des paramètres d'interface
    /// </summary>
    public partial class InterfaceSettingsData
    {
        #region Paramètres de collecte

        /// <summary>
        /// Indique si la fenêtre de progression doit être affichée pendant la collecte
        /// </summary>
        public bool ShowProgressWindow { get; set; } = true;

        /// <summary>
        /// Délai en millisecondes entre chaque étape de collecte
        /// </summary>
        public int StepDelayInMilliseconds { get; set; } = 500;

        #endregion

        #region Paramètres de fenêtre

        /// <summary>
        /// Largeur de la fenêtre principale en pixels
        /// </summary>
        public double WindowWidth { get; set; } = 500;

        /// <summary>
        /// Hauteur de la fenêtre principale en pixels
        /// </summary>
        public double WindowHeight { get; set; } = 460;

        /// <summary>
        /// Indique si l'aperçu en direct des changements de dimensions est activé
        /// </summary>
        public bool LivePreviewEnabled { get; set; } = false;

        #endregion

        // TODO: Ajouter d'autres paramètres d'interface ici au besoin
    }
}