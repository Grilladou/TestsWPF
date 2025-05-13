using System;
using System.IO;

namespace HelloWorld
{
    /// <summary>
    /// Extension partielle de la classe InterfaceSettingsData pour gérer les informations "À propos".
    /// Cette classe partielle ajoute des fonctionnalités de chargement dynamique de texte
    /// depuis un fichier externe pour la section "À propos" de l'interface.
    /// </summary>
    public partial class InterfaceSettingsData
    {
        #region Constantes pour les données À propos

        /// <summary>
        /// Nom du fichier contenant les informations "À propos"
        /// </summary>
        private const string ABOUT_FILENAME = "about.txt";

        /// <summary>
        /// Chemin par défaut pour le répertoire de configuration
        /// </summary>
        private static readonly string DEFAULT_CONFIG_DIRECTORY =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HelloWorld");

        /// <summary>
        /// Taille maximale autorisée pour le fichier about.txt (10 Ko)
        /// </summary>
        public const int MAX_FILE_SIZE_BYTES = 10 * 1024;

        #endregion

        #region Propriétés pour les données À propos

        /// <summary>
        /// Objet contenant les données "À propos" chargées depuis le fichier texte.
        /// Cette propriété n'est pas sérialisée en JSON car les données sont
        /// chargées dynamiquement depuis un fichier texte séparé.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public AboutSettingsData AboutData { get; private set; }

        /// <summary>
        /// Indique si le fichier "À propos" doit être recherché dans un répertoire personnalisé
        /// plutôt que dans le répertoire par défaut des paramètres
        /// </summary>
        public bool UseCustomAboutFilePath { get; set; } = false;

        /// <summary>
        /// Chemin personnalisé pour le fichier "À propos"
        /// Utilisé uniquement si UseCustomAboutFilePath est true
        /// </summary>
        public string CustomAboutFilePath { get; set; } = string.Empty;

        #endregion

        #region Méthodes d'initialisation et de chargement des données À propos

        /// <summary>
        /// Initialise les données "À propos" lors de la création ou du chargement des paramètres
        /// </summary>
        public void InitializeAboutData()
        {
            // Déterminer le chemin du fichier "À propos"
            string aboutFilePath = GetAboutFilePath();

            // Créer et initialiser l'objet AboutData
            AboutData = new AboutSettingsData(aboutFilePath);

            // Tenter de charger les données depuis le fichier
            LoadAboutData();
        }

        /// <summary>
        /// Charge ou recharge les données "À propos" depuis le fichier défini
        /// </summary>
        /// <returns>True si le chargement a réussi, sinon False</returns>
        public bool LoadAboutData()
        {
            // Vérifier que AboutData est initialisé
            if (AboutData == null)
            {
                InitializeAboutData();
            }

            // Mettre à jour le chemin du fichier (au cas où il aurait changé)
            AboutData.AboutFilePath = GetAboutFilePath();

            // Charger les données depuis le fichier
            return AboutData.LoadFromFile();
        }

        /// <summary>
        /// Détermine le chemin complet du fichier "À propos" en fonction des paramètres
        /// </summary>
        /// <returns>Chemin complet du fichier "À propos"</returns>
        public string GetAboutFilePath()
        {
            // Si un chemin personnalisé est configuré et non vide, l'utiliser
            if (UseCustomAboutFilePath && !string.IsNullOrEmpty(CustomAboutFilePath))
            {
                return CustomAboutFilePath;
            }
            else
            {
                // Sinon, utiliser le répertoire de configuration par défaut
                string configDirectory = DEFAULT_CONFIG_DIRECTORY;

                // Créer le répertoire s'il n'existe pas
                if (!Directory.Exists(configDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(configDirectory);
                    }
                    catch (Exception ex)
                    {
                        // En cas d'erreur, utiliser le répertoire courant
                        System.Diagnostics.Debug.WriteLine($"Erreur lors de la création du répertoire: {ex.Message}");
                        configDirectory = Directory.GetCurrentDirectory();
                    }
                }

                return Path.Combine(configDirectory, ABOUT_FILENAME);
            }
        }

        /// <summary>
        /// Vérifie si les données "À propos" contiennent du texte à afficher
        /// </summary>
        /// <returns>True si au moins une ligne contient du texte</returns>
        public bool HasAboutContent()
        {
            // S'assurer que AboutData est initialisé
            if (AboutData == null)
            {
                InitializeAboutData();
            }

            return AboutData.HasContent;
        }

        /// <summary>
        /// Recharge les données "À propos" pour refléter les modifications du fichier
        /// </summary>
        /// <returns>True si le rechargement a réussi</returns>
        public bool RefreshAboutData()
        {
            return LoadAboutData();
        }

        #endregion
    }
}