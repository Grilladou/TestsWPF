using System;
using System.Windows;
using System.Windows.Controls;
using HelloWorld.Preview;

namespace HelloWorld
{
    /// <summary>
    /// Classe d'adaptateur qui connecte l'onglet de paramètres d'interface
    /// au système de prévisualisation de fenêtre.
    /// Implémente IWindowDimensionProvider pour fournir les dimensions
    /// et gère la communication entre les deux systèmes.
    /// </summary>
    public class InterfaceSettingsPreviewAdapter : IWindowDimensionProvider, IDisposable
    {
        #region Champs privés

        // Référence à l'onglet de paramètres d'interface
        private readonly InterfaceSettingsTab _settingsTab;

        // Gestionnaire de prévisualisation
        private IWindowPreviewManager _previewManager;

        // Indique si l'adaptateur a été disposé
        private bool _isDisposed;

        #endregion

        #region Constructeur et initialisation

        /// <summary>
        /// Initialise une nouvelle instance de la classe InterfaceSettingsPreviewAdapter
        /// </summary>
        /// <param name="settingsTab">Onglet de paramètres d'interface à adapter</param>
        /// <param name="targetWindow">Fenêtre cible à prévisualiser</param>
        /// <exception cref="ArgumentNullException">Lancée si settingsTab ou targetWindow est null</exception>
        public InterfaceSettingsPreviewAdapter(InterfaceSettingsTab settingsTab, Window targetWindow)
        {
            // Vérifier que les paramètres sont valides
            if (settingsTab == null)
            {
                throw new ArgumentNullException(nameof(settingsTab), "L'onglet de paramètres ne peut pas être null");
            }

            if (targetWindow == null)
            {
                throw new ArgumentNullException(nameof(targetWindow), "La fenêtre cible ne peut pas être null");
            }

            // Stocker la référence à l'onglet de paramètres
            _settingsTab = settingsTab;

            // Créer le gestionnaire de prévisualisation
            _previewManager = new WindowPreviewManager();
            _previewManager.Initialize(targetWindow);
            _previewManager.SetDimensionProvider(this);

            // Définir le renderer et la stratégie de positionnement
            _previewManager.SetPreviewRenderer(new SimulatedPreviewRenderer());
            _previewManager.SetPositionStrategy(new SnapPositionStrategy());

            // S'abonner aux événements de l'onglet de paramètres
            SubscribeEvents();
        }

        /// <summary>
        /// S'abonne aux événements de l'onglet de paramètres
        /// </summary>
        private void SubscribeEvents()
        {
            // S'abonner à l'événement de demande de prévisualisation
            _settingsTab.PreviewDimensionsRequested += SettingsTab_PreviewDimensionsRequested;

            // S'abonner à l'événement de changement de dimensions
            _settingsTab.DimensionsChanged += SettingsTab_DimensionsChanged;

            // S'abonner à l'événement de validation des paramètres
            _settingsTab.SettingsValidated += SettingsTab_SettingsValidated;

            // S'abonner à l'événement d'application des paramètres
            _settingsTab.SettingsApplied += SettingsTab_SettingsApplied;

            // S'abonner à l'événement de réinitialisation des paramètres
            _settingsTab.ResetRequested += SettingsTab_ResetRequested;

            // S'abonner aux événements du gestionnaire de prévisualisation
            _previewManager.PreviewStarted += PreviewManager_PreviewStarted;
            _previewManager.PreviewUpdated += PreviewManager_PreviewUpdated;
            _previewManager.PreviewStopped += PreviewManager_PreviewStopped;
            _previewManager.PreviewApplied += PreviewManager_PreviewApplied;
        }

        /// <summary>
        /// Se désabonne des événements de l'onglet de paramètres
        /// </summary>
        private void UnsubscribeEvents()
        {
            // Se désabonner de l'événement de demande de prévisualisation
            _settingsTab.PreviewDimensionsRequested -= SettingsTab_PreviewDimensionsRequested;

            // Se désabonner de l'événement de changement de dimensions
            _settingsTab.DimensionsChanged -= SettingsTab_DimensionsChanged;

            // Se désabonner de l'événement de validation des paramètres
            _settingsTab.SettingsValidated -= SettingsTab_SettingsValidated;

            // Se désabonner de l'événement d'application des paramètres
            _settingsTab.SettingsApplied -= SettingsTab_SettingsApplied;

            // Se désabonner de l'événement de réinitialisation des paramètres
            _settingsTab.ResetRequested -= SettingsTab_ResetRequested;

            // Se désabonner des événements du gestionnaire de prévisualisation
            if (_previewManager != null)
            {
                _previewManager.PreviewStarted -= PreviewManager_PreviewStarted;
                _previewManager.PreviewUpdated -= PreviewManager_PreviewUpdated;
                _previewManager.PreviewStopped -= PreviewManager_PreviewStopped;
                _previewManager.PreviewApplied -= PreviewManager_PreviewApplied;
            }
        }

        #endregion

        #region Implémentation de IWindowDimensionProvider

        /// <summary>
        /// Événement déclenché lorsque les dimensions changent
        /// </summary>
        public event EventHandler<WindowDimensionEventArgs> DimensionsChanged;

        /// <summary>
        /// Obtient les dimensions actuelles fournies par la source
        /// </summary>
        /// <returns>Un Size contenant la largeur et la hauteur</returns>
        public Size GetCurrentDimensions()
        {
            try
            {
                // Récupérer les valeurs depuis les champs de texte de l'onglet
                if (double.TryParse(_settingsTab.WidthTextBox.Text, out double width) &&
                    double.TryParse(_settingsTab.HeightTextBox.Text, out double height))
                {
                    return new Size(width, height);
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération des dimensions: {ex.Message}");
            }

            // En cas d'erreur, retourner les dimensions par défaut
            return new Size(500, 400);
        }

        /// <summary>
        /// Déclenche l'événement DimensionsChanged
        /// </summary>
        /// <param name="newSize">Nouvelles dimensions</param>
        protected virtual void OnDimensionsChanged(Size newSize)
        {
            DimensionsChanged?.Invoke(this, new WindowDimensionEventArgs(newSize));
        }

        #endregion

        #region Gestionnaires d'événements de l'onglet de paramètres

        /// <summary>
        /// Gestionnaire de l'événement PreviewDimensionsRequested de l'onglet de paramètres
        /// </summary>
        private void SettingsTab_PreviewDimensionsRequested(object sender, InterfaceSettingsTab.DimensionsChangedEventArgs e)
        {
            try
            {
                // Convertir les arguments d'événement de l'onglet en dimensions WPF
                Size newSize = new Size(e.Width, e.Height);

                // Démarrer ou mettre à jour la prévisualisation
                if (_previewManager.IsPreviewActive)
                {
                    _previewManager.UpdatePreview(newSize);
                }
                else
                {
                    _previewManager.StartPreview(newSize);
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la prévisualisation des dimensions: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement DimensionsChanged de l'onglet de paramètres
        /// </summary>
        private void SettingsTab_DimensionsChanged(object sender, InterfaceSettingsTab.DimensionsChangedEventArgs e)
        {
            try
            {
                // Convertir les arguments d'événement de l'onglet en dimensions WPF
                Size newSize = new Size(e.Width, e.Height);

                // Arrêter la prévisualisation car les dimensions sont appliquées directement
                if (_previewManager.IsPreviewActive)
                {
                    _previewManager.ApplyPreviewedDimensions();
                }

                // Notifier du changement de dimensions
                OnDimensionsChanged(newSize);
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors du changement des dimensions: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement SettingsValidated de l'onglet de paramètres
        /// </summary>
        private void SettingsTab_SettingsValidated(object sender, SettingsValidationEventArgs e)
        {
            // Arrêter la prévisualisation si les paramètres sont invalides
            if (!e.IsValid && _previewManager.IsPreviewActive)
            {
                _previewManager.StopPreview();
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement SettingsApplied de l'onglet de paramètres
        /// </summary>
        private void SettingsTab_SettingsApplied(object sender, EventArgs e)
        {
            // Arrêter la prévisualisation car les paramètres sont appliqués
            if (_previewManager.IsPreviewActive)
            {
                _previewManager.StopPreview();
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement ResetRequested de l'onglet de paramètres
        /// </summary>
        private void SettingsTab_ResetRequested(object sender, EventArgs e)
        {
            // Arrêter la prévisualisation car les paramètres sont réinitialisés
            if (_previewManager.IsPreviewActive)
            {
                _previewManager.StopPreview();
            }
        }

        #endregion

        #region Gestionnaires d'événements du gestionnaire de prévisualisation

        /// <summary>
        /// Gestionnaire de l'événement PreviewStarted du gestionnaire de prévisualisation
        /// </summary>
        private void PreviewManager_PreviewStarted(object sender, WindowDimensionEventArgs e)
        {
            // Cette méthode peut être utilisée pour notifier l'interface que la prévisualisation a commencé
            System.Diagnostics.Debug.WriteLine("Prévisualisation démarrée: " + e.NewSize);
        }

        /// <summary>
        /// Gestionnaire de l'événement PreviewUpdated du gestionnaire de prévisualisation
        /// </summary>
        private void PreviewManager_PreviewUpdated(object sender, WindowDimensionEventArgs e)
        {
            // Cette méthode peut être utilisée pour notifier l'interface que la prévisualisation a été mise à jour
            System.Diagnostics.Debug.WriteLine("Prévisualisation mise à jour: " + e.NewSize);
        }

        /// <summary>
        /// Gestionnaire de l'événement PreviewStopped du gestionnaire de prévisualisation
        /// </summary>
        private void PreviewManager_PreviewStopped(object sender, WindowDimensionEventArgs e)
        {
            // Cette méthode peut être utilisée pour notifier l'interface que la prévisualisation a été arrêtée
            System.Diagnostics.Debug.WriteLine("Prévisualisation arrêtée: " + e.NewSize);
        }

        /// <summary>
        /// Gestionnaire de l'événement PreviewApplied du gestionnaire de prévisualisation
        /// </summary>
        private void PreviewManager_PreviewApplied(object sender, WindowDimensionEventArgs e)
        {
            // Cette méthode peut être utilisée pour notifier l'interface que les dimensions prévisualisées ont été appliquées
            System.Diagnostics.Debug.WriteLine("Prévisualisation appliquée: " + e.NewSize);
        }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Change le renderer utilisé pour la prévisualisation
        /// </summary>
        /// <param name="renderer">Type de renderer à utiliser</param>
        public void ChangeRenderer(PreviewRendererType renderer)
        {
            // Vérifier que l'adaptateur n'a pas été disposé
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            // Vérifier que le gestionnaire est disponible
            if (_previewManager == null)
            {
                return;
            }

            // Créer le renderer en fonction du type spécifié
            IPreviewRenderer previewRenderer;
            switch (renderer)
            {
                case PreviewRendererType.Outline:
                    previewRenderer = new OutlinePreviewRenderer();
                    break;
                case PreviewRendererType.Thumbnail:
                    previewRenderer = new ThumbnailPreviewRenderer();
                    break;
                case PreviewRendererType.Simulated:
                default:
                    previewRenderer = new SimulatedPreviewRenderer();
                    break;
            }

            // Définir le nouveau renderer
            _previewManager.SetPreviewRenderer(previewRenderer);
        }

        /// <summary>
        /// Change le type d'indicateur de dimensions utilisé pour la prévisualisation
        /// </summary>
        /// <param name="indicatorType">Type d'indicateur à utiliser</param>
        public void ChangeDimensionIndicatorType(DimensionIndicatorType indicatorType)
        {
            // Vérifier que l'adaptateur n'a pas été disposé
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            // Vérifier que le gestionnaire est disponible
            if (_previewManager == null)
            {
                System.Diagnostics.Debug.WriteLine("AVERTISSEMENT: Impossible de changer le type d'indicateur - gestionnaire non disponible");
                return;
            }

            try
            {
                // Journaliser l'opération pour faciliter le débogage
                System.Diagnostics.Debug.WriteLine($"Changement du type d'indicateur: {indicatorType}");

                // Obtenir explicitement le renderer du gestionnaire
                IPreviewRenderer currentRenderer = GetCurrentRenderer();

                if (currentRenderer == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERREUR: Impossible d'obtenir le renderer actuel");
                    return;
                }

                // Journaliser le type du renderer récupéré
                System.Diagnostics.Debug.WriteLine($"Type de renderer récupéré: {currentRenderer.GetType().Name}");

                // Définir le type d'indicateur en fonction du type de renderer
                if (currentRenderer is OutlinePreviewRenderer outlineRenderer)
                {
                    outlineRenderer.DimensionIndicatorType = indicatorType;
                    System.Diagnostics.Debug.WriteLine("Type d'indicateur défini sur OutlinePreviewRenderer");
                }
                else if (currentRenderer is ThumbnailPreviewRenderer thumbnailRenderer)
                {
                    thumbnailRenderer.DimensionIndicatorType = indicatorType;
                    System.Diagnostics.Debug.WriteLine("Type d'indicateur défini sur ThumbnailPreviewRenderer");
                }
                else if (currentRenderer is SimulatedPreviewRenderer simulatedRenderer)
                {
                    simulatedRenderer.DimensionIndicatorType = indicatorType;
                    System.Diagnostics.Debug.WriteLine("Type d'indicateur défini sur SimulatedPreviewRenderer");
                }
                else
                {
                    // Pour tout autre type de renderer
                    System.Diagnostics.Debug.WriteLine($"Type de renderer non reconnu: {currentRenderer.GetType().Name}");

                    // Essayer de définir la propriété via réflexion
                    try
                    {
                        var property = currentRenderer.GetType().GetProperty("DimensionIndicatorType");
                        if (property != null && property.CanWrite)
                        {
                            property.SetValue(currentRenderer, indicatorType);
                            System.Diagnostics.Debug.WriteLine("Type d'indicateur défini via réflexion");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("AVERTISSEMENT: Le renderer n'a pas de propriété DimensionIndicatorType accessible");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ERREUR lors de l'accès via réflexion: {ex.Message}");
                    }
                }

                // Si une prévisualisation est active, la mettre à jour
                if (_previewManager.IsPreviewActive)
                {
                    // Forcer une mise à jour de la prévisualisation avec les dimensions actuelles
                    Size currentDimensions = GetCurrentDimensions();
                    _previewManager.UpdatePreview(currentDimensions);
                    System.Diagnostics.Debug.WriteLine($"Prévisualisation mise à jour avec dimensions: {currentDimensions.Width}x{currentDimensions.Height}");
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur pour le débogage
                System.Diagnostics.Debug.WriteLine($"ERREUR lors du changement du type d'indicateur: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Récupère le renderer actuel du gestionnaire de prévisualisation
        /// </summary>
        /// <returns>Le renderer actuel ou null si indisponible</returns>
        private IPreviewRenderer GetCurrentRenderer()
        {
            if (_previewManager == null)
                return null;

            try
            {
                // Essayer d'abord d'obtenir directement le renderer via une propriété publique
                var previewManagerType = _previewManager.GetType();
                var rendererProperty = previewManagerType.GetProperty("Renderer",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (rendererProperty != null)
                {
                    return rendererProperty.GetValue(_previewManager) as IPreviewRenderer;
                }

                // Essayer ensuite de récupérer le renderer via un champ privé
                var rendererField = previewManagerType.GetField("_previewRenderer",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (rendererField != null)
                {
                    return rendererField.GetValue(_previewManager) as IPreviewRenderer;
                }

                // Si ces approches échouent, essayer d'autres noms de champs possibles
                string[] possibleFieldNames = new[] { "previewRenderer", "renderer", "_renderer", "m_previewRenderer" };

                foreach (var fieldName in possibleFieldNames)
                {
                    var field = previewManagerType.GetField(fieldName,
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (field != null)
                    {
                        return field.GetValue(_previewManager) as IPreviewRenderer;
                    }
                }

                // En dernier recours, essayer d'autres approches pour obtenir le renderer
                // Par exemple, récupérer la fenêtre de prévisualisation via réflexion
                var previewWindowField = previewManagerType.GetField("_previewWindow",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (previewWindowField != null)
                {
                    var previewWindow = previewWindowField.GetValue(_previewManager);
                    if (previewWindow != null)
                    {
                        var previewWindowType = previewWindow.GetType();
                        var windowRendererProperty = previewWindowType.GetProperty("Renderer");
                        if (windowRendererProperty != null)
                        {
                            return windowRendererProperty.GetValue(previewWindow) as IPreviewRenderer;
                        }
                    }
                }

                // Aucune approche n'a fonctionné
                System.Diagnostics.Debug.WriteLine("AVERTISSEMENT: Impossible de récupérer le renderer par réflexion");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR lors de la récupération du renderer: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Change la stratégie de positionnement utilisée pour la prévisualisation
        /// </summary>
        /// <param name="strategy">Type de stratégie à utiliser</param>
        public void ChangePositionStrategy(PositionStrategyType strategy)
        {
            // Vérifier que l'adaptateur n'a pas été disposé
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            // Vérifier que le gestionnaire est disponible
            if (_previewManager == null)
            {
                return;
            }

            // Créer la stratégie en fonction du type spécifié
            IPositionStrategy positionStrategy;
            switch (strategy)
            {
                case PositionStrategyType.Adjacent:
                    positionStrategy = new AdjacentPositionStrategy();
                    break;
                case PositionStrategyType.CenterScreen:
                    positionStrategy = new CenterScreenPositionStrategy();
                    break;
                case PositionStrategyType.Snap:
                default:
                    positionStrategy = new SnapPositionStrategy();
                    break;
            }

            // Définir la nouvelle stratégie
            _previewManager.SetPositionStrategy(positionStrategy);
        }

        /// <summary>
        /// Démarre manuellement une prévisualisation avec les dimensions actuelles
        /// </summary>
        public void StartPreview()
        {
            // Vérifier que l'adaptateur n'a pas été disposé
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            // Vérifier que le gestionnaire est disponible
            if (_previewManager == null)
            {
                return;
            }

            // Récupérer les dimensions actuelles
            Size currentSize = GetCurrentDimensions();

            // Démarrer la prévisualisation
            _previewManager.StartPreview(currentSize);
        }

        /// <summary>
        /// Arrête manuellement la prévisualisation en cours
        /// </summary>
        public void StopPreview()
        {
            // Vérifier que l'adaptateur n'a pas été disposé
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            // Vérifier que le gestionnaire est disponible
            if (_previewManager == null)
            {
                return;
            }

            // Arrêter la prévisualisation
            _previewManager.StopPreview();
        }

        #endregion

        #region Implémentation de IDisposable

        /// <summary>
        /// Libère les ressources utilisées par l'adaptateur
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Libère les ressources utilisées par l'adaptateur
        /// </summary>
        /// <param name="disposing">Indique si la méthode a été appelée directement ou par le GC</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                // Arrêter la prévisualisation en cours
                if (_previewManager != null && _previewManager.IsPreviewActive)
                {
                    _previewManager.StopPreview();
                }

                // Se désabonner des événements
                UnsubscribeEvents();

                // Disposer le gestionnaire de prévisualisation
                if (_previewManager is IDisposable disposableManager)
                {
                    disposableManager.Dispose();
                }
                _previewManager = null;
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Destructeur
        /// </summary>
        ~InterfaceSettingsPreviewAdapter()
        {
            Dispose(false);
        }

        #endregion
    }

    /// <summary>
    /// Extensions pour intégrer l'adaptateur de prévisualisation à l'onglet de paramètres d'interface
    /// </summary>
    public static class InterfaceSettingsTabExtensions
    {
        /// <summary>
        /// Clé utilisée pour stocker l'adaptateur de prévisualisation dans les données associées à l'onglet
        /// </summary>
        private const string PreviewAdapterKey = "PreviewAdapter";

        /// <summary>
        /// Active la prévisualisation pour l'onglet de paramètres d'interface
        /// </summary>
        /// <param name="settingsTab">Onglet de paramètres d'interface</param>
        /// <param name="targetWindow">Fenêtre cible à prévisualiser</param>
        /// <param name="rendererType">Type de renderer à utiliser</param>
        /// <param name="strategyType">Type de stratégie de positionnement à utiliser</param>
        /// <returns>L'onglet de paramètres d'interface (pour le chaînage de méthodes)</returns>
        public static InterfaceSettingsTab EnablePreview(
            this InterfaceSettingsTab settingsTab,
            Window targetWindow,
            PreviewRendererType rendererType = PreviewRendererType.Simulated,
            PositionStrategyType strategyType = PositionStrategyType.Snap)
        {
            // Désactiver la prévisualisation existante si elle est active
            DisablePreview(settingsTab);

            // Créer un nouvel adaptateur
            var adapter = new InterfaceSettingsPreviewAdapter(settingsTab, targetWindow);

            // Configurer l'adaptateur
            adapter.ChangeRenderer(rendererType);
            adapter.ChangePositionStrategy(strategyType);

            // Stocker l'adaptateur dans les données associées à l'onglet
            settingsTab.SetValue(FrameworkElement.TagProperty, adapter);

            return settingsTab;
        }

        /// <summary>
        /// Change le type d'indicateur de dimensions utilisé pour la prévisualisation
        /// </summary>
        /// <param name="settingsTab">Onglet de paramètres d'interface</param>
        /// <param name="indicatorType">Type d'indicateur à utiliser</param>
        /// <returns>L'onglet de paramètres d'interface (pour le chaînage de méthodes)</returns>
        public static InterfaceSettingsTab ChangeDimensionIndicatorType(
            this InterfaceSettingsTab settingsTab,
            DimensionIndicatorType indicatorType)
        {
            // Récupérer l'adaptateur existant
            var adapter = settingsTab.GetValue(FrameworkElement.TagProperty) as InterfaceSettingsPreviewAdapter;

            // Changer le type d'indicateur si l'adaptateur existe
            if (adapter != null)
            {
                adapter.ChangeDimensionIndicatorType(indicatorType);
            }

            return settingsTab;
        }

        /// <summary>
        /// Désactive la prévisualisation pour l'onglet de paramètres d'interface
        /// </summary>
        /// <param name="settingsTab">Onglet de paramètres d'interface</param>
        /// <returns>L'onglet de paramètres d'interface (pour le chaînage de méthodes)</returns>
        public static InterfaceSettingsTab DisablePreview(this InterfaceSettingsTab settingsTab)
        {
            // Récupérer l'adaptateur existant
            var adapter = settingsTab.GetValue(FrameworkElement.TagProperty) as InterfaceSettingsPreviewAdapter;

            // Disposer l'adaptateur s'il existe
            if (adapter != null)
            {
                adapter.Dispose();
                settingsTab.SetValue(FrameworkElement.TagProperty, null);
            }

            return settingsTab;
        }

        /// <summary>
        /// Change le renderer utilisé pour la prévisualisation
        /// </summary>
        /// <param name="settingsTab">Onglet de paramètres d'interface</param>
        /// <param name="rendererType">Type de renderer à utiliser</param>
        /// <returns>L'onglet de paramètres d'interface (pour le chaînage de méthodes)</returns>
        public static InterfaceSettingsTab ChangePreviewRenderer(
            this InterfaceSettingsTab settingsTab,
            PreviewRendererType rendererType)
        {
            // Récupérer l'adaptateur existant
            var adapter = settingsTab.GetValue(FrameworkElement.TagProperty) as InterfaceSettingsPreviewAdapter;

            // Changer le renderer si l'adaptateur existe
            if (adapter != null)
            {
                adapter.ChangeRenderer(rendererType);
            }

            return settingsTab;
        }

        /// <summary>
        /// Change la stratégie de positionnement utilisée pour la prévisualisation
        /// </summary>
        /// <param name="settingsTab">Onglet de paramètres d'interface</param>
        /// <param name="strategyType">Type de stratégie à utiliser</param>
        /// <returns>L'onglet de paramètres d'interface (pour le chaînage de méthodes)</returns>
        public static InterfaceSettingsTab ChangePreviewPositionStrategy(
            this InterfaceSettingsTab settingsTab,
            PositionStrategyType strategyType)
        {
            // Récupérer l'adaptateur existant
            var adapter = settingsTab.GetValue(FrameworkElement.TagProperty) as InterfaceSettingsPreviewAdapter;

            // Changer la stratégie si l'adaptateur existe
            if (adapter != null)
            {
                adapter.ChangePositionStrategy(strategyType);
            }

            return settingsTab;
        }

        /// <summary>
        /// Démarre manuellement une prévisualisation avec les dimensions actuelles
        /// </summary>
        /// <param name="settingsTab">Onglet de paramètres d'interface</param>
        /// <returns>L'onglet de paramètres d'interface (pour le chaînage de méthodes)</returns>
        public static InterfaceSettingsTab StartPreview(this InterfaceSettingsTab settingsTab)
        {
            // Récupérer l'adaptateur existant
            var adapter = settingsTab.GetValue(FrameworkElement.TagProperty) as InterfaceSettingsPreviewAdapter;

            // Démarrer la prévisualisation si l'adaptateur existe
            if (adapter != null)
            {
                adapter.StartPreview();
            }

            return settingsTab;
        }

        /// <summary>
        /// Arrête manuellement la prévisualisation en cours
        /// </summary>
        /// <param name="settingsTab">Onglet de paramètres d'interface</param>
        /// <returns>L'onglet de paramètres d'interface (pour le chaînage de méthodes)</returns>
        public static InterfaceSettingsTab StopPreview(this InterfaceSettingsTab settingsTab)
        {
            // Récupérer l'adaptateur existant
            var adapter = settingsTab.GetValue(FrameworkElement.TagProperty) as InterfaceSettingsPreviewAdapter;

            // Arrêter la prévisualisation si l'adaptateur existe
            if (adapter != null)
            {
                adapter.StopPreview();
            }

            return settingsTab;
        }
    }
}