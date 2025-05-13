using System;
using System.Windows;
using HelloWorld.Preview;

namespace HelloWorld
{
    /// <summary>
    /// Classe de coordination centrale pour le système de prévisualisation.
    /// Fournit un point d'entrée unique pour toutes les opérations de prévisualisation,
    /// assurant une cohérence entre les différents modes et contrôles.
    /// 
    /// Cette classe implémente le pattern Singleton pour garantir une coordination centralisée
    /// de toutes les opérations de prévisualisation dans l'application.
    /// </summary>
    public class PreviewCoordinator
    {
        #region Champs privés

        // Instance unique (singleton)
        private static PreviewCoordinator _instance;

        // Verrou pour l'initialisation thread-safe
        private static readonly object _lockObject = new object();

        // Fenêtre cible actuelle
        private Window _targetWindow;

        // Gestionnaire de prévisualisation actuel
        private IWindowPreviewManager _previewManager;

        // Drapeau pour éviter les mises à jour récursives
        private bool _isUpdating = false;

        // Dernières dimensions prévisualisées
        private Size _lastPreviewSize;

        // Type de prévisualisation actuel
        private PreviewRendererType _currentRendererType = PreviewRendererType.Outline;

        // Type d'indicateur de dimensions actuel
        private DimensionIndicatorType _currentIndicatorType = DimensionIndicatorType.PixelsOnly;

        // Indique si le coordinateur est initialisé
        private bool _isInitialized = false;

        #endregion

        #region Propriétés publiques

        /// <summary>
        /// Obtient l'instance unique du coordinateur (singleton)
        /// </summary>
        public static PreviewCoordinator Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new PreviewCoordinator();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Obtient une valeur indiquant si une prévisualisation est active
        /// </summary>
        public bool IsPreviewActive
        {
            get
            {
                return _previewManager != null && _previewManager.IsPreviewActive;
            }
        }

        /// <summary>
        /// Obtient ou définit le type de renderer actuel
        /// </summary>
        public PreviewRendererType CurrentRendererType
        {
            get { return _currentRendererType; }
            set
            {
                if (_currentRendererType != value)
                {
                    _currentRendererType = value;
                    UpdatePreviewRenderer();
                }
            }
        }

        /// <summary>
        /// Obtient ou définit le type d'indicateur de dimensions actuel
        /// </summary>
        public DimensionIndicatorType CurrentIndicatorType
        {
            get { return _currentIndicatorType; }
            set
            {
                if (_currentIndicatorType != value)
                {
                    _currentIndicatorType = value;
                    UpdateIndicatorType();
                }
            }
        }

        /// <summary>
        /// Obtient une valeur indiquant si le coordinateur est correctement initialisé
        /// </summary>
        public bool IsInitialized
        {
            get { return _isInitialized; }
        }

        /// <summary>
        /// Obtient la dernière taille prévisualisée
        /// </summary>
        public Size LastPreviewSize
        {
            get { return _lastPreviewSize; }
        }

        #endregion

        #region Constructeur et initialisation

        /// <summary>
        /// Constructeur privé (singleton)
        /// </summary>
        private PreviewCoordinator()
        {
            // Initialisation vide - l'initialisation réelle se fait avec Initialize()
            _isInitialized = false;
            _lastPreviewSize = Size.Empty;
            System.Diagnostics.Debug.WriteLine("PreviewCoordinator: Nouvelle instance créée");
        }

        /// <summary>
        /// Initialise le coordinateur avec la fenêtre cible
        /// </summary>
        /// <param name="targetWindow">Fenêtre cible à prévisualiser</param>
        /// <returns>True si l'initialisation a réussi, sinon False</returns>
        public bool Initialize(Window targetWindow)
        {
            // Vérifier que la fenêtre cible n'est pas null
            if (targetWindow == null)
            {
                System.Diagnostics.Debug.WriteLine("PreviewCoordinator.Initialize: targetWindow est null");
                return false;
            }

            try
            {
                // Stocker la référence à la fenêtre cible
                _targetWindow = targetWindow;

                // Récupérer ou créer le gestionnaire de prévisualisation
                _previewManager = _targetWindow.GetPreviewManager();
                if (_previewManager == null)
                {
                    _previewManager = _targetWindow.EnablePreview(
                        _currentRendererType,
                        PositionStrategyType.Smart); // Utilisation de la stratégie intelligente par défaut

                    if (_previewManager == null)
                    {
                        System.Diagnostics.Debug.WriteLine("PreviewCoordinator.Initialize: Échec de création du gestionnaire de prévisualisation");
                        return false;
                    }
                }

                // S'abonner aux événements du gestionnaire
                SubscribeToPreviewManagerEvents();

                // Configurer le type de renderer initial
                UpdatePreviewRenderer();

                // Marquer l'initialisation comme réussie
                _isInitialized = true;

                System.Diagnostics.Debug.WriteLine("PreviewCoordinator.Initialize: Initialisation réussie");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'initialisation du coordinateur de prévisualisation: {ex.Message}");
                _isInitialized = false;
                return false;
            }
        }

        /// <summary>
        /// Réinitialise le coordinateur avec une nouvelle fenêtre cible
        /// </summary>
        /// <param name="targetWindow">Nouvelle fenêtre cible à prévisualiser</param>
        /// <returns>True si la réinitialisation a réussi, sinon False</returns>
        public bool Reinitialize(Window targetWindow)
        {
            // Nettoyer les ressources existantes
            Cleanup();

            // Réinitialiser avec la nouvelle fenêtre
            return Initialize(targetWindow);
        }

        /// <summary>
        /// S'abonne aux événements du gestionnaire de prévisualisation
        /// </summary>
        private void SubscribeToPreviewManagerEvents()
        {
            // Vérifier que le gestionnaire existe
            if (_previewManager == null)
            {
                return;
            }

            // Se désabonner d'abord pour éviter les abonnements multiples
            _previewManager.PreviewStarted -= PreviewManager_PreviewStarted;
            _previewManager.PreviewStopped -= PreviewManager_PreviewStopped;
            _previewManager.PreviewUpdated -= PreviewManager_PreviewUpdated;
            _previewManager.PreviewApplied -= PreviewManager_PreviewApplied;

            // S'abonner aux événements
            _previewManager.PreviewStarted += PreviewManager_PreviewStarted;
            _previewManager.PreviewStopped += PreviewManager_PreviewStopped;
            _previewManager.PreviewUpdated += PreviewManager_PreviewUpdated;
            _previewManager.PreviewApplied += PreviewManager_PreviewApplied;

            System.Diagnostics.Debug.WriteLine("PreviewCoordinator: Événements du gestionnaire connectés");
        }

        #endregion

        #region Gestion de la prévisualisation

        /// <summary>
        /// Démarre une prévisualisation avec les dimensions spécifiées
        /// </summary>
        /// <param name="width">Largeur à prévisualiser</param>
        /// <param name="height">Hauteur à prévisualiser</param>
        /// <returns>True si la prévisualisation a démarré avec succès, sinon False</returns>
        public bool StartPreview(double width, double height)
        {
            // Vérifier que les dimensions sont valides
            if (width <= 0 || height <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"PreviewCoordinator.StartPreview: Dimensions invalides: {width}x{height}");
                return false;
            }

            return StartPreview(new Size(width, height));
        }

        /// <summary>
        /// Démarre une prévisualisation avec les dimensions spécifiées
        /// </summary>
        /// <param name="size">Dimensions à prévisualiser</param>
        /// <returns>True si la prévisualisation a démarré avec succès, sinon False</returns>
        public bool StartPreview(Size size)
        {
            // Vérifier que le gestionnaire est initialisé
            if (!_isInitialized || _targetWindow == null)
            {
                System.Diagnostics.Debug.WriteLine("PreviewCoordinator.StartPreview: Coordinateur non initialisé");

                // Tenter d'initialiser le gestionnaire automatiquement si une fenêtre cible est disponible
                if (_targetWindow != null)
                {
                    if (!Initialize(_targetWindow))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            try
            {
                // Protection contre la récursion
                if (_isUpdating)
                {
                    return false;
                }

                _isUpdating = true;

                // S'assurer que le type de renderer est correct
                UpdatePreviewRenderer();

                // Stocker les dimensions pour référence future
                _lastPreviewSize = size;

                // Démarrer la prévisualisation
                if (_previewManager.IsPreviewActive)
                {
                    _previewManager.UpdatePreview(size);
                    System.Diagnostics.Debug.WriteLine($"PreviewCoordinator.StartPreview: Prévisualisation existante mise à jour avec dimensions {size.Width}x{size.Height}");
                }
                else
                {
                    _previewManager.StartPreview(size);
                    System.Diagnostics.Debug.WriteLine($"PreviewCoordinator.StartPreview: Nouvelle prévisualisation démarrée avec dimensions {size.Width}x{size.Height}");
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du démarrage de la prévisualisation: {ex.Message}");
                return false;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// Met à jour la prévisualisation avec les nouvelles dimensions
        /// </summary>
        /// <param name="size">Nouvelles dimensions</param>
        /// <returns>True si la mise à jour a réussi, sinon False</returns>
        public bool UpdatePreview(Size size)
        {
            // Vérifier que la prévisualisation est active
            if (!IsPreviewActive)
            {
                return StartPreview(size);
            }

            try
            {
                // Protection contre la récursion
                if (_isUpdating)
                {
                    return false;
                }

                _isUpdating = true;

                // Stocker les dimensions pour référence future
                _lastPreviewSize = size;

                // Mettre à jour la prévisualisation
                _previewManager.UpdatePreview(size);

                System.Diagnostics.Debug.WriteLine($"PreviewCoordinator.UpdatePreview: Prévisualisation mise à jour avec dimensions {size.Width}x{size.Height}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la mise à jour de la prévisualisation: {ex.Message}");
                return false;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// Arrête la prévisualisation en cours
        /// </summary>
        /// <returns>True si l'arrêt a réussi, sinon False</returns>
        public bool StopPreview()
        {
            // Vérifier que la prévisualisation est active
            if (!IsPreviewActive)
            {
                return true; // Déjà arrêtée, considérer comme réussi
            }

            try
            {
                // Protection contre la récursion
                if (_isUpdating)
                {
                    return false;
                }

                _isUpdating = true;

                // Arrêter la prévisualisation
                _previewManager.StopPreview();

                System.Diagnostics.Debug.WriteLine("PreviewCoordinator.StopPreview: Prévisualisation arrêtée");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'arrêt de la prévisualisation: {ex.Message}");
                return false;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// Applique les dimensions prévisualisées à la fenêtre cible
        /// </summary>
        /// <returns>True si l'application a réussi, sinon False</returns>
        public bool ApplyPreviewedDimensions()
        {
            // Vérifier que la prévisualisation est active
            if (!IsPreviewActive)
            {
                System.Diagnostics.Debug.WriteLine("PreviewCoordinator.ApplyPreviewedDimensions: Aucune prévisualisation active");
                return false;
            }

            try
            {
                // Protection contre la récursion
                if (_isUpdating)
                {
                    return false;
                }

                _isUpdating = true;

                // Appliquer les dimensions
                _previewManager.ApplyPreviewedDimensions();

                System.Diagnostics.Debug.WriteLine($"PreviewCoordinator.ApplyPreviewedDimensions: Dimensions {_lastPreviewSize.Width}x{_lastPreviewSize.Height} appliquées");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'application des dimensions prévisualisées: {ex.Message}");
                return false;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// Met à jour le renderer de prévisualisation avec le type actuel
        /// </summary>
        /// <returns>True si la mise à jour a réussi, sinon False</returns>
        private bool UpdatePreviewRenderer()
        {
            // Vérifier que le gestionnaire est initialisé
            if (_previewManager == null || _targetWindow == null)
            {
                System.Diagnostics.Debug.WriteLine("UpdatePreviewRenderer: Gestionnaire non initialisé");
                return false;
            }

            try
            {
                // Créer un nouveau renderer du type actuel
                IPreviewRenderer renderer = PreviewRendererFactory.CreateRenderer(_currentRendererType);

                // Configurer le type d'indicateur
                ConfigureIndicatorType(renderer, _currentIndicatorType);

                // Initialiser le renderer avec la fenêtre cible
                renderer.Initialize(_targetWindow);

                // Définir le renderer dans le gestionnaire
                _previewManager.SetPreviewRenderer(renderer);

                System.Diagnostics.Debug.WriteLine($"UpdatePreviewRenderer: Renderer mis à jour avec type {_currentRendererType}");

                // Mettre à jour la prévisualisation si elle est active
                if (_previewManager.IsPreviewActive && _lastPreviewSize.Width > 0 && _lastPreviewSize.Height > 0)
                {
                    _previewManager.UpdatePreview(_lastPreviewSize);
                    System.Diagnostics.Debug.WriteLine($"UpdatePreviewRenderer: Prévisualisation mise à jour avec dimensions {_lastPreviewSize.Width}x{_lastPreviewSize.Height}");
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la mise à jour du renderer: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Met à jour le type d'indicateur de dimensions sur le renderer actuel
        /// </summary>
        /// <returns>True si la mise à jour a réussi, sinon False</returns>
        private bool UpdateIndicatorType()
        {
            // Vérifier que le gestionnaire est initialisé
            if (_previewManager == null)
            {
                System.Diagnostics.Debug.WriteLine("UpdateIndicatorType: Gestionnaire non initialisé");
                return false;
            }

            try
            {
                // Obtenir le renderer actuel via réflexion
                IPreviewRenderer renderer = GetCurrentRenderer();
                if (renderer == null)
                {
                    System.Diagnostics.Debug.WriteLine("UpdateIndicatorType: Renderer actuel non disponible");
                    return false;
                }

                // Configurer le type d'indicateur
                ConfigureIndicatorType(renderer, _currentIndicatorType);

                System.Diagnostics.Debug.WriteLine($"UpdateIndicatorType: Type d'indicateur mis à jour avec {_currentIndicatorType}");

                // Mettre à jour la prévisualisation si elle est active
                if (_previewManager.IsPreviewActive && _lastPreviewSize.Width > 0 && _lastPreviewSize.Height > 0)
                {
                    _previewManager.UpdatePreview(_lastPreviewSize);
                    System.Diagnostics.Debug.WriteLine($"UpdateIndicatorType: Prévisualisation mise à jour avec dimensions {_lastPreviewSize.Width}x{_lastPreviewSize.Height}");
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la mise à jour du type d'indicateur: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Configure le type d'indicateur de dimensions sur un renderer
        /// </summary>
        /// <param name="renderer">Renderer à configurer</param>
        /// <param name="indicatorType">Type d'indicateur à utiliser</param>
        private void ConfigureIndicatorType(IPreviewRenderer renderer, DimensionIndicatorType indicatorType)
        {
            if (renderer == null)
            {
                return;
            }

            try
            {
                // Configurer selon le type spécifique
                if (renderer is OutlinePreviewRenderer outlineRenderer)
                {
                    outlineRenderer.DimensionIndicatorType = indicatorType;
                    System.Diagnostics.Debug.WriteLine($"ConfigureIndicatorType: Indicateur {indicatorType} configuré sur OutlinePreviewRenderer");
                }
                else if (renderer is ThumbnailPreviewRenderer thumbnailRenderer)
                {
                    thumbnailRenderer.DimensionIndicatorType = indicatorType;
                    System.Diagnostics.Debug.WriteLine($"ConfigureIndicatorType: Indicateur {indicatorType} configuré sur ThumbnailPreviewRenderer");
                }
                else if (renderer is SimulatedPreviewRenderer simulatedRenderer)
                {
                    simulatedRenderer.DimensionIndicatorType = indicatorType;
                    System.Diagnostics.Debug.WriteLine($"ConfigureIndicatorType: Indicateur {indicatorType} configuré sur SimulatedPreviewRenderer");
                }
                else
                {
                    // Pour tout autre type, essayer via réflexion
                    try
                    {
                        var property = renderer.GetType().GetProperty("DimensionIndicatorType");
                        if (property != null && property.CanWrite)
                        {
                            property.SetValue(renderer, indicatorType);
                            System.Diagnostics.Debug.WriteLine($"ConfigureIndicatorType: Indicateur {indicatorType} configuré via réflexion sur {renderer.GetType().Name}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("ConfigureIndicatorType: Propriété DimensionIndicatorType inaccessible via réflexion");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur lors de l'accès via réflexion: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans ConfigureIndicatorType: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtient le renderer actuel du gestionnaire de prévisualisation
        /// </summary>
        /// <returns>Le renderer actuel ou null si non disponible</returns>
        private IPreviewRenderer GetCurrentRenderer()
        {
            if (_previewManager == null)
            {
                return null;
            }

            try
            {
                // Utiliser la réflexion pour accéder au renderer
                var previewManagerType = _previewManager.GetType();

                // Chercher d'abord via une propriété publique
                var rendererProperty = previewManagerType.GetProperty("Renderer",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (rendererProperty != null)
                {
                    var renderer = rendererProperty.GetValue(_previewManager) as IPreviewRenderer;
                    if (renderer != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetCurrentRenderer: Récupéré via propriété publique Renderer: {renderer.GetType().Name}");
                        return renderer;
                    }
                }

                // Sinon, via un champ privé
                var rendererField = previewManagerType.GetField("_previewRenderer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (rendererField != null)
                {
                    var renderer = rendererField.GetValue(_previewManager) as IPreviewRenderer;
                    if (renderer != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetCurrentRenderer: Récupéré via champ privé _previewRenderer: {renderer.GetType().Name}");
                        return renderer;
                    }
                }

                // Essayer d'autres noms possibles
                string[] possibleFieldNames = { "previewRenderer", "renderer", "_renderer", "m_previewRenderer" };

                foreach (var fieldName in possibleFieldNames)
                {
                    var field = previewManagerType.GetField(fieldName,
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (field != null)
                    {
                        var renderer = field.GetValue(_previewManager) as IPreviewRenderer;
                        if (renderer != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"GetCurrentRenderer: Récupéré via champ alternatif {fieldName}: {renderer.GetType().Name}");
                            return renderer;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("GetCurrentRenderer: Aucun renderer trouvé via réflexion");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans GetCurrentRenderer: {ex.Message}");
            }

            return null;
        }

        #endregion

        #region Évènements du gestionnaire de prévisualisation

        /// <summary>
        /// Événement déclenché lorsqu'une prévisualisation commence
        /// </summary>
        public event EventHandler<WindowDimensionEventArgs> PreviewStarted;

        /// <summary>
        /// Événement déclenché lorsqu'une prévisualisation est mise à jour
        /// </summary>
        public event EventHandler<WindowDimensionEventArgs> PreviewUpdated;

        /// <summary>
        /// Événement déclenché lorsqu'une prévisualisation se termine
        /// </summary>
        public event EventHandler<WindowDimensionEventArgs> PreviewStopped;

        /// <summary>
        /// Événement déclenché lorsque les dimensions prévisualisées sont appliquées
        /// </summary>
        public event EventHandler<WindowDimensionEventArgs> PreviewApplied;

        /// <summary>
        /// Gestionnaire de l'événement PreviewStarted du gestionnaire de prévisualisation
        /// </summary>
        private void PreviewManager_PreviewStarted(object sender, WindowDimensionEventArgs e)
        {
            try
            {
                // Stocker les dimensions
                _lastPreviewSize = e.NewSize;

                System.Diagnostics.Debug.WriteLine($"PreviewManager_PreviewStarted: Prévisualisation démarrée avec dimensions {e.Width}x{e.Height}");

                // Relayer l'événement
                PreviewStarted?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans PreviewManager_PreviewStarted: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement PreviewUpdated du gestionnaire de prévisualisation
        /// </summary>
        private void PreviewManager_PreviewUpdated(object sender, WindowDimensionEventArgs e)
        {
            try
            {
                // Stocker les dimensions
                _lastPreviewSize = e.NewSize;

                System.Diagnostics.Debug.WriteLine($"PreviewManager_PreviewUpdated: Prévisualisation mise à jour avec dimensions {e.Width}x{e.Height}");

                // Relayer l'événement
                PreviewUpdated?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans PreviewManager_PreviewUpdated: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement PreviewStopped du gestionnaire de prévisualisation
        /// </summary>
        private void PreviewManager_PreviewStopped(object sender, WindowDimensionEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PreviewManager_PreviewStopped: Prévisualisation arrêtée");

                // Relayer l'événement
                PreviewStopped?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans PreviewManager_PreviewStopped: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement PreviewApplied du gestionnaire de prévisualisation
        /// </summary>
        private void PreviewManager_PreviewApplied(object sender, WindowDimensionEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PreviewManager_PreviewApplied: Dimensions {e.Width}x{e.Height} appliquées");

                // Relayer l'événement
                PreviewApplied?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans PreviewManager_PreviewApplied: {ex.Message}");
            }
        }

        #endregion

        #region Méthodes utilitaires

        /// <summary>
        /// Détermine le type de prévisualisation à partir du mode de prévisualisation
        /// </summary>
        /// <param name="previewMode">Mode de prévisualisation</param>
        /// <returns>Type de renderer correspondant</returns>
        public static PreviewRendererType GetRendererTypeFromPreviewMode(PreviewModeType previewMode)
        {
            switch (previewMode)
            {
                case PreviewModeType.Thumbnail:
                    return PreviewRendererType.Thumbnail;
                case PreviewModeType.Outline:
                    return PreviewRendererType.Outline;
                case PreviewModeType.Full:
                    return PreviewRendererType.Simulated;
                default:
                    return PreviewRendererType.Outline; // Mode par défaut
            }
        }

        /// <summary>
        /// Détermine le mode de prévisualisation à partir du type de renderer
        /// </summary>
        /// <param name="rendererType">Type de renderer</param>
        /// <returns>Mode de prévisualisation correspondant</returns>
        public static PreviewModeType GetPreviewModeFromRendererType(PreviewRendererType rendererType)
        {
            switch (rendererType)
            {
                case PreviewRendererType.Thumbnail:
                    return PreviewModeType.Thumbnail;
                case PreviewRendererType.Outline:
                    return PreviewModeType.Outline;
                case PreviewRendererType.Simulated:
                    return PreviewModeType.Full;
                case PreviewRendererType.Simplified:
                    return PreviewModeType.Outline; // Pas de mode exact, mapper à outline
                default:
                    return PreviewModeType.Outline; // Mode par défaut
            }
        }

        /// <summary>
        /// Charge les paramètres de prévisualisation depuis les paramètres de l'application
        /// </summary>
        public void LoadFromSettings()
        {
            try
            {
                // Récupérer les paramètres depuis l'instance des paramètres de l'application
                var settings = AppSettings.Instance?.InterfaceSettings;
                if (settings == null)
                {
                    System.Diagnostics.Debug.WriteLine("LoadFromSettings: Paramètres d'interface non disponibles");
                    return;
                }

                // Définir le type de renderer
                _currentRendererType = settings.PreviewRendererType;

                // Définir le type d'indicateur
                _currentIndicatorType = settings.IndicatorsType;

                // Mettre à jour le renderer si nécessaire
                if (_previewManager != null)
                {
                    UpdatePreviewRenderer();
                }

                System.Diagnostics.Debug.WriteLine($"LoadFromSettings: Paramètres chargés - rendererType: {_currentRendererType}, indicatorType: {_currentIndicatorType}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des paramètres: {ex.Message}");
            }
        }

        /// <summary>
        /// Sauvegarde les paramètres de prévisualisation dans les paramètres de l'application
        /// </summary>
        public void SaveToSettings()
        {
            try
            {
                // Récupérer l'instance des paramètres d'interface
                var settings = AppSettings.Instance?.InterfaceSettings;
                if (settings == null)
                {
                    System.Diagnostics.Debug.WriteLine("SaveToSettings: Paramètres d'interface non disponibles");
                    return;
                }

                // Sauvegarder le type de renderer
                settings.PreviewRendererType = _currentRendererType;

                // Sauvegarder le type d'indicateur
                settings.IndicatorsType = _currentIndicatorType;

                // Synchroniser le mode de prévisualisation avec le type de renderer
                settings.PreviewMode = GetPreviewModeFromRendererType(_currentRendererType);

                System.Diagnostics.Debug.WriteLine($"SaveToSettings: Paramètres sauvegardés - rendererType: {_currentRendererType}, indicatorType: {_currentIndicatorType}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la sauvegarde des paramètres: {ex.Message}");
            }
        }

        /// <summary>
        /// Diagnostique l'état du coordinateur et de ses composants pour faciliter le débogage
        /// </summary>
        /// <returns>Chaîne contenant des informations de diagnostic</returns>
        public string DiagnoseState()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== Diagnostic du PreviewCoordinator ===");

            try
            {
                // État général du coordinateur
                report.AppendLine($"Initialisé: {_isInitialized}");
                report.AppendLine($"Fenêtre cible disponible: {(_targetWindow != null ? "Oui" : "Non")}");
                report.AppendLine($"Prévisualisation active: {IsPreviewActive}");
                report.AppendLine($"Type de renderer actuel: {_currentRendererType}");
                report.AppendLine($"Type d'indicateur actuel: {_currentIndicatorType}");

                if (_lastPreviewSize.Width > 0 && _lastPreviewSize.Height > 0)
                {
                    report.AppendLine($"Dernières dimensions prévisualisées: {_lastPreviewSize.Width}x{_lastPreviewSize.Height}");
                }
                else
                {
                    report.AppendLine("Aucune dimension prévisualisée récemment");
                }

                // Informations sur le gestionnaire
                if (_previewManager != null)
                {
                    report.AppendLine("\nInformations sur le gestionnaire de prévisualisation:");
                    report.AppendLine($"Type: {_previewManager.GetType().Name}");
                    report.AppendLine($"Est initialisé: {_previewManager.IsInitialized}");
                    report.AppendLine($"Prévisualisation active: {_previewManager.IsPreviewActive}");

                    if (_previewManager.LastPreviewedSize.Width > 0 && _previewManager.LastPreviewedSize.Height > 0)
                    {
                        report.AppendLine($"Dernières dimensions: {_previewManager.LastPreviewedSize.Width}x{_previewManager.LastPreviewedSize.Height}");
                    }

                    // Informations sur le renderer actuel
                    var renderer = GetCurrentRenderer();
                    if (renderer != null)
                    {
                        report.AppendLine("\nInformations sur le renderer actuel:");
                        report.AppendLine($"Type: {renderer.GetType().Name}");
                    }
                    else
                    {
                        report.AppendLine("\nAucun renderer actuel détecté");
                    }
                }
                else
                {
                    report.AppendLine("\nAucun gestionnaire de prévisualisation disponible");
                }

                // Suggestions de dépannage
                report.AppendLine("\nSuggestions de dépannage:");
                if (!_isInitialized)
                {
                    report.AppendLine("- Le coordinateur n'est pas initialisé. Appelez Initialize() avec une fenêtre valide.");
                }

                if (_targetWindow == null)
                {
                    report.AppendLine("- Aucune fenêtre cible définie. Assurez-vous qu'une fenêtre valide est fournie lors de l'initialisation.");
                }

                if (_previewManager == null)
                {
                    report.AppendLine("- Aucun gestionnaire de prévisualisation. Réinitialisez le coordinateur ou créez un nouveau gestionnaire.");
                }

                if (_isInitialized && _targetWindow != null && _previewManager != null && !IsPreviewActive)
                {
                    report.AppendLine("- Tout semble correctement configuré, mais aucune prévisualisation n'est active. Essayez d'appeler StartPreview() avec des dimensions valides.");
                }
            }
            catch (Exception ex)
            {
                report.AppendLine($"\nErreur lors du diagnostic: {ex.Message}");
            }

            return report.ToString();
        }

        /// <summary>
        /// Nettoie les ressources utilisées par le coordinateur
        /// </summary>
        public void Cleanup()
        {
            try
            {
                // Arrêter la prévisualisation si elle est active
                if (IsPreviewActive)
                {
                    StopPreview();
                }

                // Se désabonner des événements
                if (_previewManager != null)
                {
                    _previewManager.PreviewStarted -= PreviewManager_PreviewStarted;
                    _previewManager.PreviewStopped -= PreviewManager_PreviewStopped;
                    _previewManager.PreviewUpdated -= PreviewManager_PreviewUpdated;
                    _previewManager.PreviewApplied -= PreviewManager_PreviewApplied;
                }

                // Effacer les références
                _previewManager = null;
                _targetWindow = null;
                _isInitialized = false;

                System.Diagnostics.Debug.WriteLine("PreviewCoordinator: Ressources nettoyées");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du nettoyage: {ex.Message}");
            }
        }

        #endregion
    }
}










