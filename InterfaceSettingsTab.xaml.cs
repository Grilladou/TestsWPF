using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using WinForms = System.Windows.Forms; // Pour Screen
using HelloWorld.Preview; // Référence pour utiliser les classes de prévisualisation
using PreviewRendererType = HelloWorld.Preview.PreviewRendererType;
using PositionStrategy = HelloWorld.Preview.PositionStrategyType;

namespace HelloWorld
{
    /// <summary>
    /// Logique d'interaction pour InterfaceSettingsTab.xaml
    /// Gère l'interface utilisateur et la logique de l'onglet des paramètres d'interface
    /// </summary>
    public partial class InterfaceSettingsTab : UserControl, ISettingsTab
    {
        #region Méthodes publiques

        /// <summary>
        /// Définit la fenêtre parente pour permettre les opérations liées à la prévisualisation
        /// </summary>
        /// <param name="parentWindow">La fenêtre parente à utiliser</param>
        public void SetParentWindow(Window parentWindow)
        {
            _parentWindow = parentWindow;

            // Mettre à jour les informations d'écran après avoir défini la fenêtre parente
            UpdateScreenInfo();

            // Initialiser le coordinateur de prévisualisation avec la nouvelle fenêtre parente
            InitializePreviewCoordinator();
        }

        /// <summary>
        /// Met à jour les contraintes d'écran pour l'onglet d'interface
        /// </summary>
        public void UpdateScreenConstraints()
        {
            // Rafraîchir les limites d'écran
            if (_screenLimits != null)
            {
                _screenLimits.RefreshLimits();
            }
            else
            {
                _screenLimits = new ScreenLimits();
            }

            // Mettre à jour les plages des sliders en fonction de l'écran
            InitializeSliderRanges();

            // Mettre à jour les informations d'écran et les avertissements liés aux dimensions
            UpdateScreenInfo();
        }

        /// <summary>
        /// Met à jour les contrôles de dimensions (TextBox et Slider) avec les valeurs actuelles de la fenêtre.
        /// Cette méthode est appelée lorsque l'utilisateur redimensionne manuellement la fenêtre avec la souris.
        /// Elle protège contre les mises à jour récursives en utilisant le flag _isUpdatingControls.
        /// </summary>
        /// <param name="width">Largeur actuelle de la fenêtre (en unités logiques)</param>
        /// <param name="height">Hauteur actuelle de la fenêtre (en unités logiques)</param>
        public void UpdateDimensionControlsFromWindow(double width, double height)
        {
            try
            {
                // Si nous sommes déjà en train de mettre à jour les contrôles, sortir pour éviter les mises à jour récursives
                // Ce mécanisme est crucial pour prévenir les boucles infinies de mises à jour
                if (_isUpdatingControls)
                {
                    return;
                }

                // Vérifier que les dimensions sont valides (positives)
                if (width <= 0 || height <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Dimensions invalides pour la mise à jour des contrôles: {width}x{height}");
                    return;
                }

                // Journaliser les dimensions reçues pour le débogage
                System.Diagnostics.Debug.WriteLine($"UpdateDimensionControlsFromWindow: Dimensions reçues: {width}x{height}");

                // Les dimensions sont déjà en unités logiques, donc pas besoin de conversion
                // Utiliser la méthode existante pour mettre à jour les contrôles
                UpdateDimensionControls(width, height);

                // Vérifier si les dimensions correspondent à un preset
                CheckIfDimensionsMatchPreset();
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la mise à jour des contrôles depuis la fenêtre: {ex.Message}");
            }
        }

        #endregion

        #region Champs privés
        // Indicateur pour éviter les mises à jour récursives des contrôles
        private bool _isUpdatingControls;

        // Drapeau pour éviter les mises à jour récursives du bouton de prévisualisation
        private bool _isUpdatingPreviewButton;

        // Référence à la fenêtre parente
        private Window _parentWindow;

        // Indicateur d'initialisation
        private bool _isInitializing = true;

        /// <summary>
        /// Référence au coordinateur de prévisualisation (nouvelle approche centralisée)
        /// </summary>
        private PreviewCoordinator _previewCoordinator;

        // Valeurs par défaut
        private bool _defaultShowProgressWindow;
        private int _defaultStepDelay;
        private double _defaultWindowWidth;
        private double _defaultWindowHeight;

        // État actuel des options
        private bool _livePreviewEnabled;

        // Gestionnaire de prévisualisation
        private IWindowPreviewManager _previewManager;

        // Gestionnaire de préréglages
        private PresetManager _presetManager;

        // Timer pour les prévisualisations temporaires
        private System.Windows.Threading.DispatcherTimer _temporaryPreviewTimer;

        /// <summary>
        /// Drapeau indiquant si les dimensions chargées doivent être appliquées à la fenêtre parente
        /// ou simplement affichées dans l'interface utilisateur
        /// </summary>
        private bool _applyDimensionsOnLoad = false;

        #endregion

        #region Événements ISettingsTab
        /// <summary>
        /// Événement déclenché lorsque les paramètres sont appliqués avec succès
        /// </summary>
        public event EventHandler SettingsApplied;

        /// <summary>
        /// Événement déclenché lorsque la réinitialisation des paramètres est demandée
        /// </summary>
        public event EventHandler ResetRequested;

        /// <summary>
        /// Événement déclenché lorsque les paramètres sont validés
        /// </summary>
        public event EventHandler<SettingsValidationEventArgs> SettingsValidated;

        // Gestionnaire des limites d'écran pour la validation des dimensions
        private ScreenLimits _screenLimits;

        /// <summary>
        /// Déclenche l'événement PreviewDimensionsRequested
        /// </summary>
        /// <param name="e">Arguments de l'événement contenant les dimensions</param>

        protected virtual void OnPreviewDimensionsRequested(DimensionsChangedEventArgs e)
        {
            // Déclencher l'événement s'il y a des abonnés
            PreviewDimensionsRequested?.Invoke(this, e);

            // Ajouter la journalisation pour le débogage
            System.Diagnostics.Debug.WriteLine($"OnPreviewDimensionsRequested: Événement déclenché avec dimensions {e.Width}x{e.Height}");
        }

        /// <summary>
        /// Déclenche l'événement PreviewDimensionsRequested avec les dimensions spécifiées
        /// </summary>
        /// <param name="width">Largeur à prévisualiser (en unités logiques)</param>
        /// <param name="height">Hauteur à prévisualiser (en unités logiques)</param>
        protected virtual void RequestPreviewDimensions(double width, double height)
        {
            // Créer les arguments d'événement avec les dimensions
            var args = new DimensionsChangedEventArgs(width, height);

            // Utiliser la méthode existante pour déclencher l'événement
            OnPreviewDimensionsRequested(args);
        }

        #endregion

        #region Constructeur

        /// <summary>
        /// Constructeur de l'onglet de paramètres d'interface
        /// </summary>
        public InterfaceSettingsTab()
        {
            InitializeComponent();

            // Initialiser le gestionnaire des limites d'écran
            _screenLimits = new ScreenLimits();

            // Initialiser le gestionnaire de préréglages
            _presetManager = new PresetManager();

            // Initialiser le timer pour les prévisualisations temporaires
            _temporaryPreviewTimer = new System.Windows.Threading.DispatcherTimer();
            _temporaryPreviewTimer.Tick += TemporaryPreviewTimer_Tick;

            _isUpdatingPreviewButton = false;

            // Initialiser les plages des sliders en fonction de l'écran
            InitializeSliderRanges();

            // S'assurer que le séparateur de la section À propos est initialement masqué
            // jusqu'à ce que la méthode UpdateAboutSection() soit appelée et détermine sa visibilité
            if (AboutSeparator != null)
            {
                AboutSeparator.Visibility = Visibility.Collapsed;
            }

            // Initialiser le coordinateur de prévisualisation
            try
            {
                _previewCoordinator = PreviewCoordinator.Instance;
                System.Diagnostics.Debug.WriteLine("Coordinateur de prévisualisation initialisé");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'initialisation du coordinateur de prévisualisation: {ex.Message}");
                // Continuer sans coordinateur - les méthodes vérifieront sa disponibilité avant utilisation
            }

            // Initialisation terminée
            _isInitializing = false;

            // S'abonner à l'événement de changement des paramètres pour mettre à jour la section "À propos"
            AppSettings.Instance.ModuleChanged += OnModuleChanged;

            // Connecter les contrôles de dimensions au gestionnaire de prévisualisation
            ConnectDimensionControls();

            // Connecter le ComboBox des positions sauvegardées au gestionnaire de préréglages
            _presetManager.ConnectToComboBox(SavedPositionsComboBox);

            // S'abonner à l'événement de sélection du ComboBox des positions mémorisées
            SavedPositionsComboBox.SelectionChanged += SavedPositionsComboBox_SelectionChanged;

            // S'abonner à l'événement de sélection du ComboBox des indicateurs
            IndicatorsComboBox.SelectionChanged += IndicatorsComboBox_SelectionChanged;

            // Configurer les gestionnaires d'événements pour les contrôles liés à la prévisualisation
            // Cette ligne est essentielle pour la mise à jour en temps réel des boutons radio du type de rendu
            SetupPreviewControlsEventHandlers();
        }

        /// <summary>
        /// Libère les ressources et se désabonne des événements lorsque le contrôle est déchargé
        /// </summary>
        public void Cleanup()
        {
            try
            {
                // Se désabonner de l'événement pour éviter les fuites de mémoire
                if (AppSettings.Instance != null)
                {
                    AppSettings.Instance.ModuleChanged -= OnModuleChanged;
                }

                // Se désabonner de l'événement SelectionChanged du ComboBox
                if (SavedPositionsComboBox != null)
                {
                    SavedPositionsComboBox.SelectionChanged -= SavedPositionsComboBox_SelectionChanged;
                }

                // Se désabonner des événements des boutons radio de prévisualisation
                if (ThumbnailModeRadio != null)
                {
                    ThumbnailModeRadio.Checked -= PreviewModeRadio_Checked;
                }

                if (OutlineModeRadio != null)
                {
                    OutlineModeRadio.Checked -= PreviewModeRadio_Checked;
                }

                if (FullModeRadio != null)
                {
                    FullModeRadio.Checked -= PreviewModeRadio_Checked;
                }

                // Se désabonner des événements du ComboBox des indicateurs
                if (IndicatorsComboBox != null)
                {
                    IndicatorsComboBox.SelectionChanged -= IndicatorsComboBox_SelectionChanged;
                }

                // Se désabonner des événements du gestionnaire de prévisualisation
                UnsubscribePreviewEvents();

                // Nettoyer le gestionnaire de prévisualisation
                if (_previewManager != null && _previewManager is IDisposable disposable)
                {
                    disposable.Dispose();
                    _previewManager = null;
                }

                // Arrêter le timer pour les prévisualisations temporaires
                if (_temporaryPreviewTimer != null)
                {
                    _temporaryPreviewTimer.Stop();
                    _temporaryPreviewTimer.Tick -= TemporaryPreviewTimer_Tick;
                    _temporaryPreviewTimer = null;
                }

                // Se désabonner des événements du coordinateur de prévisualisation
                if (_previewCoordinator != null)
                {
                    _previewCoordinator.PreviewStarted -= PreviewCoordinator_PreviewStarted;
                    _previewCoordinator.PreviewStopped -= PreviewCoordinator_PreviewStopped;
                    _previewCoordinator.PreviewUpdated -= PreviewCoordinator_PreviewUpdated;
                    _previewCoordinator.PreviewApplied -= PreviewCoordinator_PreviewApplied;
                }

                // Nettoyer l'adaptateur de prévisualisation pour éviter les doublons
                CleanupPreviewAdapter();
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur pour le débogage sans perturber l'application
                System.Diagnostics.Debug.WriteLine($"ERREUR dans Cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Méthodes pour la gestion des données "À propos"

        /// <summary>
        /// Met à jour l'interface utilisateur avec les données "À propos" chargées depuis le fichier texte
        /// et gère la visibilité du séparateur associé. Gère également le formatage Markdown léger.
        /// </summary>
        private void UpdateAboutSection()
        {
            // Vérification préalable pour éviter les exceptions si les éléments d'interface n'ont pas été initialisés
            if (AboutBorder == null || AboutLine1 == null || AboutLine2 == null || AboutLine3 == null || AboutSeparator == null)
            {
                // Journaliser l'erreur pour faciliter le débogage
                System.Diagnostics.Debug.WriteLine("AVERTISSEMENT: Les éléments d'interface pour la section 'À propos' ne sont pas tous initialisés");
                return;
            }

            try
            {
                // Récupérer l'instance des paramètres d'interface
                var interfaceSettings = AppSettings.Instance.InterfaceSettings;

                // État qui indique si la section À propos doit être visible
                bool shouldShowAboutSection = false;

                // Vérifier si AboutData est disponible et s'il contient du texte
                if (interfaceSettings.AboutData != null && interfaceSettings.HasAboutContent())
                {
                    // Configurer la ligne 1
                    if (!string.IsNullOrEmpty(interfaceSettings.AboutData.Line1))
                    {
                        // Appliquer le formatage Markdown à la ligne 1
                        ApplyFormattedText(AboutLine1, interfaceSettings.AboutData.Line1);
                        AboutLine1.Visibility = Visibility.Visible;
                        shouldShowAboutSection = true;
                    }
                    else
                    {
                        AboutLine1.Visibility = Visibility.Collapsed;
                    }

                    // Configurer la ligne 2
                    if (!string.IsNullOrEmpty(interfaceSettings.AboutData.Line2))
                    {
                        // Appliquer le formatage Markdown à la ligne 2
                        ApplyFormattedText(AboutLine2, interfaceSettings.AboutData.Line2);
                        AboutLine2.Visibility = Visibility.Visible;
                        shouldShowAboutSection = true;
                    }
                    else
                    {
                        AboutLine2.Visibility = Visibility.Collapsed;
                    }

                    // Configurer la ligne 3
                    if (!string.IsNullOrEmpty(interfaceSettings.AboutData.Line3))
                    {
                        // Appliquer le formatage Markdown à la ligne 3
                        ApplyFormattedText(AboutLine3, interfaceSettings.AboutData.Line3);
                        AboutLine3.Visibility = Visibility.Visible;
                        shouldShowAboutSection = true;
                    }
                    else
                    {
                        AboutLine3.Visibility = Visibility.Collapsed;
                    }
                }

                // Appliquer la visibilité à la section À propos et au séparateur associé
                if (shouldShowAboutSection)
                {
                    // Afficher le rectangle "À propos" et le séparateur si au moins une ligne est visible
                    AboutBorder.Visibility = Visibility.Visible;
                    AboutSeparator.Visibility = Visibility.Visible;
                }
                else
                {
                    // Masquer le rectangle "À propos" et le séparateur si aucune donnée n'est disponible
                    AboutBorder.Visibility = Visibility.Collapsed;
                    AboutSeparator.Visibility = Visibility.Collapsed;

                    // S'assurer que toutes les lignes sont également masquées
                    AboutLine1.Visibility = Visibility.Collapsed;
                    AboutLine2.Visibility = Visibility.Collapsed;
                    AboutLine3.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                // Capturer et journaliser toute exception qui pourrait survenir
                // pour éviter que l'interface ne plante si quelque chose se passe mal
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la mise à jour de la section 'À propos': {ex.Message}");

                // Par sécurité, masquer tout en cas d'erreur
                AboutBorder.Visibility = Visibility.Collapsed;
                AboutSeparator.Visibility = Visibility.Collapsed;
                AboutLine1.Visibility = Visibility.Collapsed;
                AboutLine2.Visibility = Visibility.Collapsed;
                AboutLine3.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Applique le formatage Markdown léger au texte et met à jour le TextBlock
        /// en tenant compte des caractères échappés (\* et \#)
        /// </summary>
        /// <param name="textBlock">Le TextBlock à mettre à jour</param>
        /// <param name="markdownText">Le texte avec formatage Markdown</param>
        private void ApplyFormattedText(TextBlock textBlock, string markdownText)
        {
            // Vérifier les paramètres
            if (textBlock == null || string.IsNullOrEmpty(markdownText))
                return;

            // Effacer le contenu actuel
            textBlock.Inlines.Clear();

            try
            {
                // Créer une expression régulière pour rechercher le formatage Markdown
                // - Texte en gras : **texte** (non précédé par \)
                // - Texte en italique : *texte* (non précédé par \)
                // Utiliser une lookbehind negative pour exclure les cas où l'astérisque est échappé
                string pattern = @"(?<!\\)(\*\*(.+?)\*\*)|(?<!\\)(\*(.+?)\*)";

                // Position actuelle dans la chaîne
                int currentPosition = 0;

                // Rechercher toutes les correspondances
                var matches = Regex.Matches(markdownText, pattern);

                if (matches.Count == 0)
                {
                    // Aucun formatage trouvé, ajouter le texte tel quel
                    // mais transformer quand même les caractères échappés
                    string processedText = markdownText
                        .Replace("\\*", "*")
                        .Replace("\\#", "#")
                        .Replace("\\\\", "\\");

                    textBlock.Inlines.Add(new Run(processedText));
                    return;
                }

                // Traiter chaque correspondance
                foreach (Match match in matches)
                {
                    // Ajouter le texte non formaté avant la correspondance, avec traitement des caractères échappés
                    if (match.Index > currentPosition)
                    {
                        string textBefore = markdownText.Substring(currentPosition, match.Index - currentPosition);
                        // Remplacer les caractères échappés dans cette portion
                        textBefore = textBefore
                            .Replace("\\*", "*")
                            .Replace("\\#", "#")
                            .Replace("\\\\", "\\");
                        textBlock.Inlines.Add(new Run(textBefore));
                    }

                    // Déterminer le type de formatage
                    if (match.Value.StartsWith("**") && match.Value.EndsWith("**"))
                    {
                        // Texte en gras
                        string boldText = match.Value.Substring(2, match.Value.Length - 4);
                        // Traiter aussi les caractères échappés dans le texte en gras
                        boldText = boldText
                            .Replace("\\*", "*")
                            .Replace("\\#", "#")
                            .Replace("\\\\", "\\");
                        textBlock.Inlines.Add(new Bold(new Run(boldText)));
                    }
                    else if (match.Value.StartsWith("*") && match.Value.EndsWith("*"))
                    {
                        // Texte en italique
                        string italicText = match.Value.Substring(1, match.Value.Length - 2);
                        // Traiter aussi les caractères échappés dans le texte en italique
                        italicText = italicText
                            .Replace("\\*", "*")
                            .Replace("\\#", "#")
                            .Replace("\\\\", "\\");
                        textBlock.Inlines.Add(new Italic(new Run(italicText)));
                    }

                    // Mettre à jour la position courante
                    currentPosition = match.Index + match.Length;
                }

                // Ajouter le texte restant après la dernière correspondance
                if (currentPosition < markdownText.Length)
                {
                    string textAfter = markdownText.Substring(currentPosition);
                    // Remplacer les caractères échappés dans la dernière portion
                    textAfter = textAfter
                        .Replace("\\*", "*")
                        .Replace("\\#", "#")
                        .Replace("\\\\", "\\");
                    textBlock.Inlines.Add(new Run(textAfter));
                }
            }
            catch (Exception ex)
            {
                // En cas d'erreur, afficher le texte brut mais quand même avec les caractères échappés traités
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'application du formatage Markdown: {ex.Message}");
                textBlock.Inlines.Clear();

                // Remplacer les caractères échappés dans le texte brut
                string processedText = markdownText
                    .Replace("\\*", "*")
                    .Replace("\\#", "#")
                    .Replace("\\\\", "\\");

                textBlock.Inlines.Add(new Run(processedText));
            }
        }

        /// <summary>
        /// Recharge les données "À propos" depuis le fichier et met à jour l'interface
        /// en gérant également la visibilité du séparateur associé.
        /// </summary>
        private void RefreshAboutData()
        {
            try
            {
                // Récupérer l'instance des paramètres d'interface
                var interfaceSettings = AppSettings.Instance.InterfaceSettings;

                // Recharger les données depuis le fichier
                interfaceSettings.RefreshAboutData();

                // Mettre à jour l'interface (cette méthode gère maintenant aussi la visibilité du séparateur)
                UpdateAboutSection();
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur pour faciliter le débogage
                System.Diagnostics.Debug.WriteLine($"Erreur lors du rafraîchissement des données 'À propos': {ex.Message}");

                // Par sécurité, masquer le séparateur et la section en cas d'erreur
                if (AboutBorder != null) AboutBorder.Visibility = Visibility.Collapsed;
                if (AboutSeparator != null) AboutSeparator.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Configure les gestionnaires d'événements pour la prévisualisation en temps réel
        /// en fonction de l'état de la case à cocher "Aperçu en temps réel"
        /// </summary>
        private void SetupLivePreviewEventHandlers()
        {
            try
            {
                // Vérifier que la fenêtre parente est disponible
                if (_parentWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERREUR: Fenêtre parente non disponible pour la configuration de la prévisualisation en temps réel");
                    return;
                }

                // Récupérer le gestionnaire de prévisualisation global
                IWindowPreviewManager previewManager = _parentWindow.GetPreviewManager();

                // Si le gestionnaire n'existe pas, en créer un nouveau
                if (previewManager == null)
                {
                    // Obtenir les paramètres actuels
                    var settings = AppSettings.Instance.InterfaceSettings;

                    // Créer un nouveau gestionnaire
                    previewManager = _parentWindow.EnablePreview(
                        settings.PreviewRendererType,
                        settings.PositionStrategyType);

                    System.Diagnostics.Debug.WriteLine("SetupLivePreviewEventHandlers: Nouveau gestionnaire de prévisualisation créé pour la fenêtre parente");
                }

                // Vérifier que les contrôles de dimensions existent
                if (WidthTextBox == null || HeightTextBox == null || WidthSlider == null || HeightSlider == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERREUR: Un ou plusieurs contrôles de dimensions sont null");
                    return;
                }

                // Utiliser la valeur de _livePreviewEnabled pour déterminer le comportement
                if (_livePreviewEnabled)
                {
                    System.Diagnostics.Debug.WriteLine("Activation de la prévisualisation en temps réel");

                    // Connecter les contrôles de dimensions au gestionnaire de prévisualisation
                    // pour une mise à jour en temps réel
                    _parentWindow.ConnectDimensionControls(
                        WidthTextBox,
                        HeightTextBox,
                        WidthSlider,
                        HeightSlider,
                        true // livePreview = true pour activer la mise à jour en temps réel
                    );

                    // Configurer un fournisseur de dimensions personnalisé
                    var provider = new ControlBasedDimensionProvider(
                        WidthTextBox, HeightTextBox, WidthSlider, HeightSlider);

                    // Appliquer le fournisseur au gestionnaire
                    previewManager.SetDimensionProvider(provider);

                    // S'abonner aux événements du fournisseur de dimensions pour mettre à jour la prévisualisation
                    provider.DimensionsChanged += (s, args) => {
                        if (previewManager.IsPreviewActive)
                        {
                            previewManager.UpdatePreview(args.NewSize);
                        }
                        else
                        {
                            previewManager.StartPreview(args.NewSize);
                        }
                    };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Désactivation de la prévisualisation en temps réel");

                    // Arrêter la prévisualisation active si elle existe
                    if (previewManager.IsPreviewActive)
                    {
                        previewManager.StopPreview();
                    }

                    // Déconnecter les contrôles pour éviter les mises à jour automatiques
                    // mais garder les gestionnaires d'événements standard pour les interactions manuelles
                    _parentWindow.ConnectDimensionControls(
                        WidthTextBox,
                        HeightTextBox,
                        WidthSlider,
                        HeightSlider,
                        false // livePreview = false pour désactiver la mise à jour en temps réel
                    );
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur pour le débogage
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la configuration des contrôles de prévisualisation: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement ModuleChanged pour mettre à jour l'interface si les données "À propos" changent
        /// </summary>
        private void OnModuleChanged(object sender, AppSettings.SettingsModuleChangedEventArgs e)
        {
            // Vérifier si c'est le module d'interface qui a changé
            if (e.ModuleName == "InterfaceSettings")
            {
                // Mettre à jour la section "À propos" au cas où elle aurait changé
                UpdateAboutSection();
            }
        }

        #endregion

        #region Méthodes ISettingsTab

        /// <summary>
        /// Charge les paramètres actuels dans l'interface utilisateur
        /// Implémentation de l'interface ISettingsTab
        /// </summary>
        public void LoadCurrentSettings()
        {
            // Appelle la surcharge avec la valeur par défaut (false)
            LoadCurrentSettings(false);
        }

        /// <summary>
        /// Charge les paramètres actuels dans l'interface utilisateur en tenant compte
        /// de l'état maximisé de la fenêtre.
        /// </summary>
        /// <param name="applyToParent">Indique si les dimensions doivent être appliquées à la fenêtre parente</param>
        public void LoadCurrentSettings(bool applyToParent)
        {
            try
            {
                // Stocker le drapeau pour éviter d'appliquer les dimensions lors du chargement initial
                // sauf si explicitement demandé
                _applyDimensionsOnLoad = applyToParent;

                // Éviter les mises à jour récursives
                _isUpdatingControls = true;

                // Récupérer l'instance des paramètres d'interface
                var interfaceSettings = AppSettings.Instance.InterfaceSettings;

                // Variables pour les dimensions à afficher dans les contrôles
                double displayWidth = interfaceSettings.WindowWidth;
                double displayHeight = interfaceSettings.WindowHeight;

                // Vérifier si la fenêtre parente est maximisée
                bool isParentMaximized = (_parentWindow != null && _parentWindow.WindowState == WindowState.Maximized);

                // Si la fenêtre parente est maximisée, utiliser ses dimensions actuelles pour l'affichage
                // mais conserver les dimensions normales dans les paramètres
                if (isParentMaximized && _parentWindow != null)
                {
                    // Utiliser les dimensions actuelles de la fenêtre maximisée pour l'affichage
                    displayWidth = _parentWindow.ActualWidth;
                    displayHeight = _parentWindow.ActualHeight;
                }

                // Charger les valeurs dans les contrôles
                ShowProgressWindowCheckBox.IsChecked = interfaceSettings.ShowProgressWindow;
                StepDelayTextBox.Text = interfaceSettings.StepDelayInMilliseconds.ToString();

                // Initialiser les plages des sliders en fonction de l'écran
                InitializeSliderRanges();

                // Mettre à jour les contrôles de dimensions avec les valeurs à afficher
                UpdateDimensionControls(displayWidth, displayHeight);

                // Options
                LivePreviewCheckBox.IsChecked = interfaceSettings.LivePreviewEnabled;

                // Utiliser les vraies valeurs par défaut de InterfaceSettingsData au lieu des valeurs actuelles
                var defaultData = new InterfaceSettingsData();
                _defaultShowProgressWindow = defaultData.ShowProgressWindow;
                _defaultStepDelay = defaultData.StepDelayInMilliseconds;
                _defaultWindowWidth = defaultData.WindowWidth;
                _defaultWindowHeight = defaultData.WindowHeight;

                // Uniquement suivre l'état actuel des options
                _livePreviewEnabled = interfaceSettings.LivePreviewEnabled;

                // Mettre à jour la section "À propos"
                UpdateAboutSection();

                // Intégrer le chargement des paramètres de prévisualisation
                IntegrateLoadPreviewSettings();
            }
            finally
            {
                _isUpdatingControls = false;

                // Réinitialiser le drapeau d'application une fois terminé
                // pour éviter des effets de bord inattendus
                _applyDimensionsOnLoad = false;
            }

            // Mettre à jour les informations d'écran
            UpdateScreenInfo();

            // Configurer les gestionnaires d'événements après que toutes les mises à jour UI sont terminées
            try
            {
                SetupLivePreviewEventHandlers();

                // S'assurer que les gestionnaires d'événements des contrôles de prévisualisation sont configurés
                // Cette ligne est importante pour garantir que les connexions sont toujours établies,
                // même si l'instance est réutilisée ou si les contrôles sont recréés dynamiquement
                SetupPreviewControlsEventHandlers();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la configuration des gestionnaires d'événements: {ex.Message}");
            }

            // Mettre à jour le ComboBox des Presets
            CheckIfDimensionsMatchPreset();

            // Ajouter une indication visuelle si la fenêtre est en état maximisé
            UpdateMaximizedIndicator();
        }

        /// <summary>
        /// Applique les paramètres modifiés
        /// </summary>
        /// <returns>True si l'application des paramètres a réussi, sinon False</returns>
        public bool ApplySettings()
        {
            string errorMessage;
            if (!ValidateSettings(out errorMessage))
            {
                MessageBox.Show(errorMessage, "Erreur de validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            try
            {
                // Récupérer l'instance des paramètres d'interface
                var interfaceSettings = AppSettings.Instance.InterfaceSettings;

                // Appliquer les paramètres de collecte
                interfaceSettings.ShowProgressWindow = ShowProgressWindowCheckBox.IsChecked.GetValueOrDefault(true);

                // Convertir et valider la valeur du délai
                if (int.TryParse(StepDelayTextBox.Text, out int delay))
                {
                    interfaceSettings.StepDelayInMilliseconds = delay;
                }

                // Appliquer les dimensions de la fenêtre
                if (double.TryParse(WidthTextBox.Text, out double width) &&
                    double.TryParse(HeightTextBox.Text, out double height))
                {
                    // La validation dans ValidateSettings garantit déjà que les dimensions sont valides,
                    // donc pas besoin d'ajustement supplémentaire ici

                    // Sauvegarder les nouvelles dimensions dans les paramètres
                    interfaceSettings.WindowWidth = width;
                    interfaceSettings.WindowHeight = height;
                }

                // Appliquer les options
                interfaceSettings.LivePreviewEnabled = _livePreviewEnabled;

                // Intégrer l'application des paramètres de prévisualisation
                IntegrateApplyPreviewSettings();

                // Rafraîchir les données "À propos" avant de sauvegarder
                interfaceSettings.RefreshAboutData();

                // Appliquer les paramètres au collecteur d'informations
                interfaceSettings.ApplyToBaseInfoCollector();

                // Sauvegarder les paramètres
                interfaceSettings.Save();

                // Si des dimensions ont été modifiées, les communiquer
                if (_parentWindow != null)
                {
                    // Vérifier si la fenêtre parente est maximisée
                    bool wasMaximized = (_parentWindow.WindowState == WindowState.Maximized);

                    if (wasMaximized)
                    {
                        try
                        {
                            // NOUVEAU CODE : Informer la fenêtre principale que des dimensions ont été appliquées
                            // pendant l'état maximalisé pour qu'elles soient restaurées plus tard
                            MainWindow mainWindow = _parentWindow as MainWindow;
                            if (mainWindow != null)
                            {
                                // Appeler la méthode publique créée dans MainWindow.xaml.cs
                                mainWindow.SetDimensionsAppliedWhileMaximized();

                                // Journaliser pour le débogage
                                System.Diagnostics.Debug.WriteLine("Flag _dimensionsAppliedWhileMaximized notifié à la fenêtre principale");
                            }

                            // Message spécifique si la fenêtre était maximisée
                            MessageBox.Show(
                                "Les nouvelles dimensions seront appliquées lorsque la fenêtre sera restaurée manuellement.",
                                "Dimensions enregistrées",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            // Journaliser l'erreur pour le débogage
                            System.Diagnostics.Debug.WriteLine($"Erreur lors de la notification des dimensions appliquées : {ex.Message}");

                            // Afficher quand même un message pour informer l'utilisateur
                            MessageBox.Show(
                                "Les nouvelles dimensions ont été enregistrées mais pourraient ne pas être appliquées correctement à la restauration.",
                                "Avertissement",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        // Si la fenêtre n'est pas maximisée, appliquer directement les nouvelles dimensions
                        _parentWindow.Width = interfaceSettings.WindowWidth;
                        _parentWindow.Height = interfaceSettings.WindowHeight;
                    }
                }

                // Déclencher l'événement SettingsApplied
                OnSettingsApplied();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'application des paramètres : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Réinitialise les paramètres à leurs valeurs par défaut
        /// </summary>
        public void ResetSettings()
        {
            try
            {
                // Éviter les mises à jour récursives
                _isUpdatingControls = true;

                // ===== SECTION 1: OPTIONS DE CHARGEMENT =====
                // Réinitialiser les valeurs des contrôles
                ShowProgressWindowCheckBox.IsChecked = _defaultShowProgressWindow;
                StepDelayTextBox.Text = _defaultStepDelay.ToString();

                // ===== SECTION 2: POSITIONS ET DIMENSIONS =====
                // Réinitialiser les dimensions aux valeurs par défaut
                UpdateDimensionControls(_defaultWindowWidth, _defaultWindowHeight);

                // Réinitialiser le ComboBox des positions mémorisées
                if (SavedPositionsComboBox != null)
                {
                    // Sélectionner le premier élément
                    if (SavedPositionsComboBox.Items.Count > 0)
                    {
                        SavedPositionsComboBox.SelectedIndex = 0;
                    }
                }

                // ===== SECTION 3: PRÉVISUALISATION =====
                // Réinitialiser l'aperçu en temps réel (désactivé par défaut)
                LivePreviewCheckBox.IsChecked = false;
                _livePreviewEnabled = false;

                // [NOUVEAU] Intégrer la réinitialisation des paramètres de prévisualisation
                IntegrateResetPreviewSettings();

                // Mettre à jour l'interface utilisateur pour refléter les changements
                // et s'assurer que les avertissements sont mis à jour
                UpdateScreenInfo();

                // Déclencher l'événement ResetRequested pour notifier les autres composants
                OnResetRequested();
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur de manière sécurisée
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la réinitialisation des paramètres: {ex.Message}");
                MessageBox.Show($"Erreur lors de la réinitialisation des paramètres: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Toujours réactiver les mises à jour des contrôles à la fin
                _isUpdatingControls = false;
            }
        }

        /// <summary>
        /// Méthode auxiliaire pour réinitialiser les options de prévisualisation
        /// </summary>
        private void ResetPreviewOptions()
        {
            // Réinitialiser l'aperçu en temps réel
            LivePreviewCheckBox.IsChecked = false;
            _livePreviewEnabled = false;

            // Réinitialiser le type d'indicateurs
            if (IndicatorsComboBox != null)
            {
                IndicatorsComboBox.SelectedIndex = 0;
            }

            // Réinitialiser le mode d'affichage
            if (ThumbnailModeRadio != null)
            {
                ThumbnailModeRadio.IsChecked = true;
                if (OutlineModeRadio != null) OutlineModeRadio.IsChecked = false;
                if (FullModeRadio != null) FullModeRadio.IsChecked = false;
            }

            // Réinitialiser le slider de durée temporaire
            if (TemporaryDurationSlider != null)
            {
                TemporaryDurationSlider.Value = 5;
            }

            // Réinitialiser l'option d'affichage des zones d'accrochage
            if (ShowSnapZonesCheckBox != null)
            {
                ShowSnapZonesCheckBox.IsChecked = false;
            }

            // Arrêter toute prévisualisation en cours
            if (_previewManager != null && _previewManager.IsPreviewActive)
            {
                _previewManager.StopPreview();
            }
        }

        /// <summary>
        /// Teste les paramètres actuels
        /// </summary>
        public void TestSettings()
        {
            try
            {
                // Tester les paramètres (par exemple, prévisualiser les dimensions)
                if (double.TryParse(WidthTextBox.Text, out double width) &&
                    double.TryParse(HeightTextBox.Text, out double height))
                {
                    // Créer une prévisualisation temporaire
                    PreviewDimensions(width, height);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du test des paramètres : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Vérifie si les paramètres sont valides
        /// </summary>
        /// <param name="errorMessage">Message d'erreur en cas d'échec</param>
        /// <returns>True si les paramètres sont valides, sinon False</returns>
        public bool ValidateSettings(out string errorMessage)
        {
            errorMessage = string.Empty;

            // Valider le délai entre les étapes
            if (!int.TryParse(StepDelayTextBox.Text, out int delay) || delay < 0)
            {
                errorMessage = "Le délai entre les étapes doit être un nombre entier positif.";
                return false;
            }

            // Valider les dimensions de la fenêtre
            if (!double.TryParse(WidthTextBox.Text, out double width) || width <= 0)
            {
                errorMessage = "La largeur doit être un nombre positif.";
                return false;
            }

            if (!double.TryParse(HeightTextBox.Text, out double height) || height <= 0)
            {
                errorMessage = "La hauteur doit être un nombre positif.";
                return false;
            }

            // S'assurer que le gestionnaire des limites d'écran est initialisé et à jour
            if (_screenLimits == null)
            {
                _screenLimits = new ScreenLimits();
            }
            else
            {
                // Rafraîchir les limites pour prendre en compte les changements d'écran possibles
                _screenLimits.RefreshLimits();
            }

            // Utiliser la classe ScreenLimits pour valider les dimensions
            if (!_screenLimits.ValidateDimensions(width, height, out errorMessage))
            {
                return false;
            }

            // Valider les paramètres de prévisualisation
            if (!IntegrateValidatePreviewSettings(ref errorMessage))
                return false;

            // Déclencher l'événement SettingsValidated si tout est valide
            OnSettingsValidated(true);

            return true;
        }
        #endregion

        #region Méthodes d'événements de l'interface ISettingsTab

        /// <summary>
        /// Déclenche l'événement SettingsApplied
        /// </summary>
        protected virtual void OnSettingsApplied()
        {
            SettingsApplied?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Déclenche l'événement ResetRequested
        /// </summary>
        protected virtual void OnResetRequested()
        {
            ResetRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Déclenche l'événement SettingsValidated
        /// </summary>
        protected virtual void OnSettingsValidated(bool isValid, string errorMessage = null)
        {
            SettingsValidated?.Invoke(this, new SettingsValidationEventArgs(isValid, errorMessage));
        }
        #endregion

        #region Configuration du système de prévisualisation

        /// <summary>
        /// Initialise le coordinateur de prévisualisation avec la fenêtre cible
        /// et s'abonne aux événements pertinents.
        /// </summary>
        private void InitializePreviewCoordinator()
        {
            try
            {
                // Vérifier que la fenêtre parente et le coordinateur existent
                if (_parentWindow == null || _previewCoordinator == null)
                {
                    System.Diagnostics.Debug.WriteLine("InitializePreviewCoordinator: _parentWindow ou _previewCoordinator est null");
                    return;
                }

                // Initialiser le coordinateur avec la fenêtre cible s'il n'est pas déjà initialisé
                if (!_previewCoordinator.IsInitialized)
                {
                    if (!_previewCoordinator.Initialize(_parentWindow))
                    {
                        System.Diagnostics.Debug.WriteLine("Échec de l'initialisation du coordinateur de prévisualisation");
                        return;
                    }
                    System.Diagnostics.Debug.WriteLine("InitializePreviewCoordinator: Coordinateur initialisé avec la fenêtre cible");
                }

                // Se désabonner d'abord pour éviter les abonnements multiples
                _previewCoordinator.PreviewStarted -= PreviewCoordinator_PreviewStarted;
                _previewCoordinator.PreviewStopped -= PreviewCoordinator_PreviewStopped;
                _previewCoordinator.PreviewUpdated -= PreviewCoordinator_PreviewUpdated;
                _previewCoordinator.PreviewApplied -= PreviewCoordinator_PreviewApplied;

                // S'abonner aux événements du coordinateur
                _previewCoordinator.PreviewStarted += PreviewCoordinator_PreviewStarted;
                _previewCoordinator.PreviewStopped += PreviewCoordinator_PreviewStopped;
                _previewCoordinator.PreviewUpdated += PreviewCoordinator_PreviewUpdated;
                _previewCoordinator.PreviewApplied += PreviewCoordinator_PreviewApplied;

                // Charger les paramètres depuis les réglages de l'application
                _previewCoordinator.LoadFromSettings();

                System.Diagnostics.Debug.WriteLine("InitializePreviewCoordinator: Configuration du coordinateur terminée");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR dans InitializePreviewCoordinator: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement PreviewStarted du coordinateur de prévisualisation
        /// </summary>
        private void PreviewCoordinator_PreviewStarted(object sender, WindowDimensionEventArgs e)
        {
            try
            {
                // Mise à jour de l'état de tous les boutons pour indiquer qu'une prévisualisation est active
                UpdateAllPreviewButtonsState(true);
                System.Diagnostics.Debug.WriteLine($"PreviewCoordinator_PreviewStarted: Prévisualisation démarrée avec dimensions {e.Width}x{e.Height}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR dans PreviewCoordinator_PreviewStarted: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement PreviewStopped du coordinateur de prévisualisation
        /// </summary>
        private void PreviewCoordinator_PreviewStopped(object sender, WindowDimensionEventArgs e)
        {
            try
            {
                // Mise à jour de l'état de tous les boutons pour indiquer que la prévisualisation est terminée
                UpdateAllPreviewButtonsState(false);

                // Réactiver le bouton temporaire s'il était désactivé
                System.Diagnostics.Debug.WriteLine($"PreviewCoordinator_PreviewStopped: Prévisualisation arrêtée");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR dans PreviewCoordinator_PreviewStopped: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement PreviewUpdated du coordinateur de prévisualisation
        /// </summary>
        private void PreviewCoordinator_PreviewUpdated(object sender, WindowDimensionEventArgs e)
        {
            try
            {
                // Journaliser la mise à jour
                System.Diagnostics.Debug.WriteLine($"PreviewCoordinator_PreviewUpdated: Prévisualisation mise à jour avec dimensions {e.Width}x{e.Height}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR dans PreviewCoordinator_PreviewUpdated: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement PreviewApplied du coordinateur de prévisualisation
        /// </summary>
        private void PreviewCoordinator_PreviewApplied(object sender, WindowDimensionEventArgs e)
        {
            try
            {
                // Mettre à jour les contrôles avec les dimensions appliquées
                UpdateDimensionControls(e.Width, e.Height);
                System.Diagnostics.Debug.WriteLine($"PreviewCoordinator_PreviewApplied: Dimensions appliquées {e.Width}x{e.Height}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR dans PreviewCoordinator_PreviewApplied: {ex.Message}");
            }
        }

        /// <summary>
        /// Met à jour le type de renderer utilisé pour la prévisualisation
        /// en fonction des boutons radio sélectionnés.
        /// Utilise le gestionnaire global de prévisualisation pour assurer la cohérence.
        /// </summary>
        private void UpdatePreviewRendererType()
        {
            try
            {
                // Journaliser le début de la méthode pour le débogage
                System.Diagnostics.Debug.WriteLine("UpdatePreviewRendererType: Mise à jour du type de renderer");

                // Vérifier que la fenêtre parente existe
                if (_parentWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("UpdatePreviewRendererType: _parentWindow est null");
                    return;
                }

                // Récupérer ou créer le gestionnaire global de prévisualisation
                IWindowPreviewManager previewManager = _parentWindow.GetPreviewManager();
                if (previewManager == null)
                {
                    // Si aucun gestionnaire n'existe, en créer un nouveau
                    previewManager = _parentWindow.EnablePreview();
                    System.Diagnostics.Debug.WriteLine("UpdatePreviewRendererType: Nouveau gestionnaire global créé");
                }

                // Vérifier que le gestionnaire a été obtenu correctement
                if (previewManager == null)
                {
                    System.Diagnostics.Debug.WriteLine("UpdatePreviewRendererType: ERREUR: Échec de récupération/création du gestionnaire de prévisualisation");
                    return;
                }

                // Déterminer le type de renderer en fonction des contrôles radio
                PreviewRendererType rendererType = PreviewRendererType.Outline; // Par défaut

                // Vérifier que les boutons radio ne sont pas null avant d'accéder à leurs propriétés
                if (ThumbnailModeRadio != null && ThumbnailModeRadio.IsChecked == true)
                    rendererType = PreviewRendererType.Thumbnail;
                else if (OutlineModeRadio != null && OutlineModeRadio.IsChecked == true)
                    rendererType = PreviewRendererType.Outline;
                else if (FullModeRadio != null && FullModeRadio.IsChecked == true)
                    rendererType = PreviewRendererType.Simulated;

                // Créer un nouveau renderer du type demandé
                IPreviewRenderer renderer = PreviewRendererFactory.CreateRenderer(rendererType);

                // Vérifier que le renderer a été créé correctement
                if (renderer == null)
                {
                    System.Diagnostics.Debug.WriteLine("UpdatePreviewRendererType: ERREUR: Échec de création du renderer");
                    return;
                }

                // Configurer le renderer avec le type d'indicateur de dimensions actuel
                ConfigureDimensionIndicatorType(renderer, GetCurrentIndicatorType());

                // Définir le nouveau renderer dans le gestionnaire global
                previewManager.SetPreviewRenderer(renderer);

                // Journaliser le type de renderer pour le débogage
                System.Diagnostics.Debug.WriteLine($"UpdatePreviewRendererType: Type de renderer configuré: {rendererType}");

                // Si une prévisualisation est active, la mettre à jour avec le nouveau renderer
                if (previewManager.IsPreviewActive)
                {
                    // Récupérer les dimensions actuelles
                    Size currentSize = GetCurrentPreviewDimensions();

                    // Mettre à jour la prévisualisation
                    previewManager.UpdatePreview(currentSize);
                    System.Diagnostics.Debug.WriteLine($"UpdatePreviewRendererType: Prévisualisation mise à jour avec dimensions {currentSize.Width}x{currentSize.Height}");
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur de manière sécurisée
                System.Diagnostics.Debug.WriteLine($"ERREUR dans UpdatePreviewRendererType: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // Propager l'exception pour permettre une gestion centralisée dans la méthode appelante
                throw;
            }
        }

        /// <summary>
        /// Obtient le type d'indicateur de dimensions actuellement sélectionné
        /// </summary>
        /// <returns>Le type d'indicateur actuel ou PixelsOnly par défaut</returns>
        private DimensionIndicatorType GetCurrentIndicatorType()
        {
            try
            {
                // Si le ComboBox existe et a une sélection valide
                if (IndicatorsComboBox != null && IndicatorsComboBox.SelectedIndex >= 0 && IndicatorsComboBox.SelectedIndex < 3)
                {
                    return (DimensionIndicatorType)IndicatorsComboBox.SelectedIndex;
                }

                // Sinon, essayer de récupérer depuis les paramètres
                if (AppSettings.Instance != null && AppSettings.Instance.InterfaceSettings != null)
                {
                    return AppSettings.Instance.InterfaceSettings.IndicatorsType;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR dans GetCurrentIndicatorType: {ex.Message}");
            }

            // Valeur par défaut en cas d'erreur
            return DimensionIndicatorType.PixelsOnly;
        }

        /// <summary>
        /// Obtient les dimensions actuelles pour la prévisualisation
        /// depuis les contrôles TextBox de largeur et hauteur.
        /// </summary>
        /// <returns>Les dimensions actuelles ou une taille par défaut en cas d'erreur</returns>
        private Size GetCurrentPreviewDimensions()
        {
            try
            {
                // Récupérer les valeurs des TextBox
                if (WidthTextBox != null && HeightTextBox != null &&
                    double.TryParse(WidthTextBox.Text, out double width) &&
                    double.TryParse(HeightTextBox.Text, out double height))
                {
                    return new Size(width, height);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR dans GetCurrentPreviewDimensions: {ex.Message}");
            }

            // Valeurs par défaut en cas d'erreur
            return new Size(500, 400);
        }

        /// <summary>
        /// Connecte les contrôles de dimensions au gestionnaire de prévisualisation
        /// et s'assure que celui-ci est correctement initialisé en utilisant le gestionnaire global.
        /// </summary>
        /// <returns>True si l'initialisation a réussi, sinon False</returns>
        private bool ConnectDimensionControls()
        {
            try
            {
                // Vérifier si une fenêtre parente a été définie
                if (_parentWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("ConnectDimensionControls: Impossible d'initialiser le système de prévisualisation car la fenêtre parente n'est pas définie");
                    return false;
                }

                // MODIFICATION IMPORTANTE: Obtenir d'abord le gestionnaire global existant
                // avant d'en créer un nouveau pour garantir l'unicité du gestionnaire
                IWindowPreviewManager globalManager = _parentWindow.GetPreviewManager();

                if (globalManager == null)
                {
                    // Si aucun gestionnaire global n'existe, en créer un nouveau
                    globalManager = WindowPreviewManagerExtensions.EnablePreview(
                        _parentWindow,
                        PreviewRendererType.Outline,  // Type de rendu visuel
                        PositionStrategy.Snap);       // Stratégie de positionnement

                    System.Diagnostics.Debug.WriteLine("ConnectDimensionControls: Nouveau gestionnaire global créé");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ConnectDimensionControls: Gestionnaire global existant récupéré");
                }

                // Vérifier que le gestionnaire a été obtenu correctement
                if (globalManager == null)
                {
                    System.Diagnostics.Debug.WriteLine("ConnectDimensionControls: ERREUR: Échec de récupération/création du gestionnaire de prévisualisation");
                    return false;
                }

                // Stocker le gestionnaire global comme référence membre de classe
                _previewManager = globalManager;

                // MODIFICATION CRITIQUE: Toujours se désabonner avant de s'abonner pour éviter
                // les abonnements multiples et les fuites mémoire
                _previewManager.PreviewStarted -= PreviewManager_PreviewStarted;
                _previewManager.PreviewStopped -= PreviewManager_PreviewStopped;
                _previewManager.PreviewApplied -= PreviewManager_PreviewApplied;

                // S'abonner aux événements du gestionnaire de prévisualisation
                _previewManager.PreviewStarted += PreviewManager_PreviewStarted;
                _previewManager.PreviewStopped += PreviewManager_PreviewStopped;
                _previewManager.PreviewApplied += PreviewManager_PreviewApplied;

                System.Diagnostics.Debug.WriteLine("ConnectDimensionControls: Événements attachés au gestionnaire global");

                // Configurer le renderer avec le type approprié selon les contrôles radio
                UpdatePreviewRendererType();

                return true;
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur de manière sécurisée
                System.Diagnostics.Debug.WriteLine($"ERREUR lors de la configuration du système de prévisualisation : {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // Ne pas propager l'exception, retourner False pour indiquer l'échec
                return false;
            }
        }

        /// <summary>
        /// Crée une prévisualisation des dimensions spécifiées
        /// </summary>
        /// <param name="width">Largeur à prévisualiser</param>
        /// <param name="height">Hauteur à prévisualiser</param>
        private void PreviewDimensions(double width, double height)
        {
            if (_parentWindow == null || _previewManager == null)
                return;

            try
            {
                // Démarrer ou mettre à jour la prévisualisation
                if (_previewManager.IsPreviewActive)
                {
                    _previewManager.UpdatePreview(new Size(width, height));
                }
                else
                {
                    _previewManager.StartPreview(new Size(width, height));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la prévisualisation des dimensions : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Crée une prévisualisation temporaire avec un délai d'expiration
        /// </summary>
        /// <param name="width">Largeur à prévisualiser</param>
        /// <param name="height">Hauteur à prévisualiser</param>
        /// <param name="durationInSeconds">Durée de la prévisualisation en secondes</param>
        private void PreviewDimensionsTemporary(double width, double height, double durationInSeconds)
        {
            // Vérifier les paramètres d'entrée
            if (width <= 0 || height <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"AVERTISSEMENT: Dimensions invalides pour la prévisualisation: {width}x{height}");

                // Informer l'utilisateur de l'erreur
                MessageBox.Show(
                    "Les dimensions fournies sont invalides. Veuillez saisir des valeurs numériques positives.",
                    "Dimensions invalides",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (durationInSeconds <= 0)
            {
                System.Diagnostics.Debug.WriteLine("AVERTISSEMENT: Durée de prévisualisation invalide");
                durationInSeconds = 5; // Valeur par défaut si la durée est invalide
            }

            // Vérification du gestionnaire et de la fenêtre parente
            if (_parentWindow == null)
            {
                System.Diagnostics.Debug.WriteLine("ERREUR: Fenêtre parente non définie");

                // Informer l'utilisateur de l'erreur
                MessageBox.Show(
                    "Impossible de prévisualiser : la fenêtre parente n'est pas définie.",
                    "Erreur de prévisualisation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            if (_previewManager == null)
            {
                System.Diagnostics.Debug.WriteLine("ERREUR: Gestionnaire de prévisualisation non initialisé");

                // Tentative de réinitialisation du gestionnaire
                ConnectDimensionControls();

                // Vérifier à nouveau si l'initialisation a réussi
                if (_previewManager == null)
                {
                    MessageBox.Show(
                        "Impossible d'initialiser le système de prévisualisation",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                // Journaliser les informations de débogage
                System.Diagnostics.Debug.WriteLine($"Démarrage de la prévisualisation temporaire: {width}x{height} pour {durationInSeconds} secondes");

                // Arrêter le timer existant s'il est en cours
                if (_temporaryPreviewTimer != null && _temporaryPreviewTimer.IsEnabled)
                {
                    _temporaryPreviewTimer.Stop();
                    System.Diagnostics.Debug.WriteLine("Timer de prévisualisation précédent arrêté");
                }

                // Arrêter toute prévisualisation en cours
                if (_previewManager.IsPreviewActive)
                {
                    _previewManager.StopPreview();
                    System.Diagnostics.Debug.WriteLine("Prévisualisation en cours arrêtée");
                }

                // S'assurer que le PreviewRendererType est correctement configuré pour afficher les dimensions
                // Vérifier la disponibilité d'un adaptateur pour la configuration du renderer
                var adapter = this.GetValue(FrameworkElement.TagProperty) as InterfaceSettingsPreviewAdapter;
                if (adapter != null)
                {
                    // Déterminer le type de renderer en fonction des contrôles radio
                    PreviewRendererType rendererType = PreviewRendererType.Outline; // Utiliser Outline pour une meilleure visibilité des dimensions

                    if (ThumbnailModeRadio != null && ThumbnailModeRadio.IsChecked == true)
                    {
                        rendererType = PreviewRendererType.Thumbnail;
                    }
                    else if (FullModeRadio != null && FullModeRadio.IsChecked == true)
                    {
                        rendererType = PreviewRendererType.Simulated;
                    }

                    // Appliquer le renderer
                    adapter.ChangeRenderer(rendererType);
                    System.Diagnostics.Debug.WriteLine($"Renderer configuré sur: {rendererType}");
                }

                // Arrondir les dimensions pour une meilleure présentation
                double roundedWidth = Math.Round(width);
                double roundedHeight = Math.Round(height);

                // Démarrer une nouvelle prévisualisation
                _previewManager.StartPreview(new Size(roundedWidth, roundedHeight));
                System.Diagnostics.Debug.WriteLine($"Nouvelle prévisualisation démarrée avec dimensions {roundedWidth}x{roundedHeight}");

                // Configurer et démarrer le timer
                _temporaryPreviewTimer.Interval = TimeSpan.FromSeconds(durationInSeconds);
                _temporaryPreviewTimer.Start();

                System.Diagnostics.Debug.WriteLine($"Timer démarré pour {durationInSeconds} secondes");
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"ERREUR lors de la prévisualisation temporaire : {ex.Message}");

                // Afficher un message à l'utilisateur
                MessageBox.Show(
                    $"Erreur lors de la prévisualisation temporaire : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement Tick du timer de prévisualisation temporaire.
        /// Arrête la prévisualisation et réactive les boutons de manière synchronisée.
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void TemporaryPreviewTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Timer de prévisualisation temporaire expiré");

                // Arrêter le timer
                if (_temporaryPreviewTimer != null && _temporaryPreviewTimer.IsEnabled)
                {
                    _temporaryPreviewTimer.Stop();
                    System.Diagnostics.Debug.WriteLine("Timer arrêté");
                }

                // Approche 1: Utiliser le coordinateur de prévisualisation si disponible
                if (_previewCoordinator != null && _previewCoordinator.IsInitialized && _previewCoordinator.IsPreviewActive)
                {
                    // Arrêter la prévisualisation via le coordinateur
                    _previewCoordinator.StopPreview();
                    System.Diagnostics.Debug.WriteLine("Prévisualisation temporaire arrêtée via coordinateur après expiration du timer");
                }
                // Approche 2: Utiliser le gestionnaire global si le coordinateur n'est pas disponible
                else if (_previewManager != null && _previewManager.IsPreviewActive)
                {
                    // Arrêter la prévisualisation via le gestionnaire global
                    _previewManager.StopPreview();
                    System.Diagnostics.Debug.WriteLine("Prévisualisation temporaire arrêtée via gestionnaire global après expiration du timer");
                }

                // Mise à jour de l'état des boutons de prévisualisation pour les réactiver de façon cohérente
                UpdateAllPreviewButtonsState(false);
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur sans perturber l'expérience utilisateur
                System.Diagnostics.Debug.WriteLine($"ERREUR dans TemporaryPreviewTimer_Tick: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // S'assurer que les boutons sont réactivés même en cas d'erreur générale dans la méthode
                try
                {
                    UpdateAllPreviewButtonsState(false);
                }
                catch
                {
                    // Ignorer toute erreur pendant la récupération d'erreur
                    // pour éviter les exceptions en cascade
                }
            }
        }

        #endregion

        #region Gestionnaires d'événements du gestionnaire de prévisualisation

        /// <summary>
        /// Gestionnaire de l'événement PreviewStarted du gestionnaire de prévisualisation.
        /// Met à jour l'interface pour indiquer qu'une prévisualisation est en cours.
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement contenant les dimensions</param>
        private void PreviewManager_PreviewStarted(object sender, WindowDimensionEventArgs e)
        {
            try
            {
                // Mise à jour de l'état de tous les boutons pour indiquer qu'une prévisualisation est en cours
                UpdateAllPreviewButtonsState(true);
                System.Diagnostics.Debug.WriteLine($"PreviewManager_PreviewStarted: État des boutons mis à jour ({e.Width}x{e.Height})");
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur sans perturber l'application
                System.Diagnostics.Debug.WriteLine($"ERREUR dans PreviewManager_PreviewStarted: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement PreviewStopped du gestionnaire de prévisualisation.
        /// Met à jour l'interface pour indiquer que la prévisualisation est terminée.
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement contenant les dimensions</param>
        private void PreviewManager_PreviewStopped(object sender, WindowDimensionEventArgs e)
        {
            try
            {
                // Mettre à jour l'état de tous les boutons pour indiquer que la prévisualisation est terminée
                UpdateAllPreviewButtonsState(false);
                System.Diagnostics.Debug.WriteLine($"PreviewManager_PreviewStopped: État des boutons mis à jour ({e.Width}x{e.Height})");
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur sans perturber l'application
                System.Diagnostics.Debug.WriteLine($"ERREUR dans PreviewManager_PreviewStopped: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement PreviewApplied du gestionnaire de prévisualisation.
        /// Met à jour les contrôles de dimensions avec les dimensions appliquées.
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement contenant les dimensions</param>
        private void PreviewManager_PreviewApplied(object sender, WindowDimensionEventArgs e)
        {
            try
            {
                // Mettre à jour les contrôles avec les dimensions appliquées
                UpdateDimensionControls(e.Width, e.Height);
                System.Diagnostics.Debug.WriteLine($"PreviewManager_PreviewApplied: Dimensions mises à jour ({e.Width}x{e.Height})");
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur sans perturber l'application
                System.Diagnostics.Debug.WriteLine($"ERREUR dans PreviewManager_PreviewApplied: {ex.Message}");
            }
        }

        #endregion

        #region Nettoyage des ressources

        /// <summary>
        /// Se désabonne des événements du gestionnaire de prévisualisation
        /// pour éviter les fuites mémoire et références circulaires.
        /// À appeler lors du déchargement du contrôle ou dans Cleanup().
        /// </summary>
        private void UnsubscribePreviewEvents()
        {
            try
            {
                // Vérifier si on a une référence au gestionnaire
                if (_previewManager != null)
                {
                    // Se désabonner des événements
                    _previewManager.PreviewStarted -= PreviewManager_PreviewStarted;
                    _previewManager.PreviewStopped -= PreviewManager_PreviewStopped;
                    _previewManager.PreviewApplied -= PreviewManager_PreviewApplied;

                    System.Diagnostics.Debug.WriteLine("UnsubscribePreviewEvents: Désabonnement des événements réussi");
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur sans perturber l'application
                System.Diagnostics.Debug.WriteLine($"ERREUR dans UnsubscribePreviewEvents: {ex.Message}");
            }
        }

        #endregion


        #region Gestionnaires d'événements de l'interface utilisateur

        /// <summary>
        /// Gestionnaire de l'événement SelectionChanged pour le ComboBox des positions mémorisées
        /// Cette méthode met à jour les contrôles de dimensions lorsqu'un preset est sélectionné
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void SavedPositionsComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Ignorer l'événement si on est en train de mettre à jour les contrôles
            // pour éviter les mises à jour récursives
            if (_isUpdatingControls)
                return;

            // Vérifier si l'élément sélectionné est valide (non null et non vide)
            string selectedPreset = SavedPositionsComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedPreset) || _presetManager == null)
                return;

            // Si la sélection est l'élément par défaut (texte d'invite), ne rien faire
            // Cette vérification est importante si vous avez un élément par défaut dans votre ComboBox
            if (selectedPreset == "Sélectionner une position mémorisée...")
                return;

            // Obtenir les dimensions correspondant au preset sélectionné
            Size presetSize = _presetManager.GetPreset(selectedPreset);
            if (presetSize == Size.Empty)
                return;

            // Mettre à jour les contrôles de dimensions (TextBox et Slider) avec les dimensions du preset
            UpdateDimensionControls(presetSize.Width, presetSize.Height);
        }

        /// <summary>
        /// Gestionnaire de l'événement pour le bouton d'exportation des positions
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void ExportPositionsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Vérifier que le gestionnaire de presets est disponible
                if (_presetManager == null)
                {
                    MessageBox.Show(
                        "Le gestionnaire de positions n'est pas initialisé.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Vérifier qu'il y a des positions à exporter
                var presetNames = _presetManager.GetPresetNames().ToList();
                if (presetNames.Count == 0)
                {
                    MessageBox.Show(
                        "Aucune position mémorisée n'est disponible pour l'exportation.",
                        "Information",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Créer une boîte de dialogue pour sauvegarder le fichier
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Exporter les positions mémorisées",
                    Filter = "Fichiers JSON (*.json)|*.json|Tous les fichiers (*.*)|*.*",
                    DefaultExt = ".json",
                    FileName = "positions_memorisees.json"
                };

                // Afficher la boîte de dialogue
                bool? result = saveFileDialog.ShowDialog();

                // Si l'utilisateur a cliqué sur OK
                if (result == true)
                {
                    // Récupérer le chemin du fichier sélectionné
                    string filePath = saveFileDialog.FileName;

                    // Exporter les positions
                    bool success = _presetManager.ExportPresetsToFile(filePath);

                    // Afficher un message de confirmation ou d'erreur
                    if (success)
                    {
                        MessageBox.Show(
                            $"Les positions mémorisées ont été exportées avec succès dans le fichier :\n{filePath}",
                            "Exportation réussie",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Une erreur s'est produite lors de l'exportation des positions mémorisées.",
                            "Erreur",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'exportation des positions : {ex.Message}");

                // Afficher un message d'erreur
                MessageBox.Show(
                    $"Une erreur inattendue s'est produite lors de l'exportation des positions :\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement pour le bouton d'importation des positions
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void ImportPositionsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Vérifier que le gestionnaire de presets est disponible
                if (_presetManager == null)
                {
                    MessageBox.Show(
                        "Le gestionnaire de positions n'est pas initialisé.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Créer une boîte de dialogue pour ouvrir le fichier
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Importer des positions mémorisées",
                    Filter = "Fichiers JSON (*.json)|*.json|Tous les fichiers (*.*)|*.*",
                    DefaultExt = ".json"
                };

                // Afficher la boîte de dialogue
                bool? result = openFileDialog.ShowDialog();

                // Si l'utilisateur a cliqué sur OK
                if (result == true)
                {
                    // Récupérer le chemin du fichier sélectionné
                    string filePath = openFileDialog.FileName;

                    // Demander à l'utilisateur s'il veut fusionner ou remplacer les positions existantes
                    MessageBoxResult mergeResult = MessageBox.Show(
                        "Voulez-vous fusionner les positions importées avec les positions existantes ?\n\n" +
                        "- Cliquez sur Oui pour ajouter les nouvelles positions aux positions existantes\n" +
                        "- Cliquez sur Non pour remplacer toutes les positions existantes par les positions importées\n\n" +
                        "Note: Les positions par défaut ne seront pas supprimées.",
                        "Mode d'importation",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    // Si l'utilisateur a annulé
                    if (mergeResult == MessageBoxResult.Cancel)
                        return;

                    // Déterminer le mode d'importation
                    bool mergeWithExisting = (mergeResult == MessageBoxResult.Yes);

                    // Importer les positions
                    bool success = _presetManager.ImportPresetsFromFile(filePath, mergeWithExisting);

                    // Mettre à jour le ComboBox
                    _presetManager.ConnectToComboBox(SavedPositionsComboBox);

                    // Afficher un message de confirmation ou d'erreur
                    if (success)
                    {
                        MessageBox.Show(
                            $"Les positions mémorisées ont été importées avec succès depuis le fichier :\n{filePath}",
                            "Importation réussie",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Une erreur s'est produite lors de l'importation des positions mémorisées. " +
                            "Vérifiez que le fichier est valide et contient des positions mémorisées.",
                            "Erreur",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'importation des positions : {ex.Message}");

                // Afficher un message d'erreur
                MessageBox.Show(
                    $"Une erreur inattendue s'est produite lors de l'importation des positions :\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement pour le bouton de prévisualisation.
        /// Démarre ou arrête la prévisualisation des dimensions de la fenêtre
        /// en utilisant de préférence le coordinateur central de prévisualisation.
        /// Synchronise l'état des boutons pour maintenir une cohérence dans l'interface.
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obtenir les dimensions actuelles
                double width, height;
                if (!double.TryParse(WidthTextBox.Text, out width) ||
                    !double.TryParse(HeightTextBox.Text, out height))
                {
                    System.Diagnostics.Debug.WriteLine("PreviewButton_Click: Dimensions invalides");
                    MessageBox.Show(
                        "Les dimensions spécifiées sont invalides. Veuillez saisir des valeurs numériques valides.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Valider que les dimensions sont positives
                if (width <= 0 || height <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"PreviewButton_Click: Dimensions négatives ou nulles: {width}x{height}");
                    MessageBox.Show(
                        "Les dimensions doivent être des valeurs positives.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Approche 1: Utiliser le coordinateur de prévisualisation si disponible (approche préférée)
                if (_previewCoordinator != null && _previewCoordinator.IsInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("PreviewButton_Click: Utilisation du coordinateur de prévisualisation");

                    // S'assurer que la configuration est à jour
                    UpdatePreviewConfiguration();

                    // Démarrer ou arrêter la prévisualisation via le coordinateur
                    if (_previewCoordinator.IsPreviewActive)
                    {
                        // Arrêter la prévisualisation
                        _previewCoordinator.StopPreview();
                        System.Diagnostics.Debug.WriteLine("PreviewButton_Click: Arrêt de la prévisualisation via coordinateur");

                        // Mettre à jour l'état de tous les boutons de prévisualisation
                        UpdateAllPreviewButtonsState(false);
                    }
                    else
                    {
                        // Démarrer la prévisualisation
                        _previewCoordinator.StartPreview(new Size(width, height));
                        System.Diagnostics.Debug.WriteLine($"PreviewButton_Click: Démarrage de la prévisualisation via coordinateur avec dimensions {width}x{height}");

                        // Mettre à jour l'état de tous les boutons de prévisualisation
                        UpdateAllPreviewButtonsState(true);
                    }

                    return;
                }

                // Approche 2: Utiliser le gestionnaire global si le coordinateur n'est pas disponible
                // Cette partie reprend la logique existante comme solution de secours

                // Vérifier si un gestionnaire existe et l'initialiser si nécessaire
                if (_parentWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("PreviewButton_Click: _parentWindow est null");
                    MessageBox.Show(
                        "La fenêtre parente n'est pas définie. Impossible d'initialiser la prévisualisation.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Récupérer ou créer le gestionnaire global de prévisualisation
                IWindowPreviewManager previewManager = _parentWindow.GetPreviewManager();
                if (previewManager == null)
                {
                    // Obtenir les paramètres actuels
                    var settings = AppSettings.Instance.InterfaceSettings;

                    // Créer un nouveau gestionnaire avec les paramètres actuels
                    previewManager = _parentWindow.EnablePreview(
                        settings.PreviewRendererType,
                        settings.PositionStrategyType);

                    // Mettre à jour la configuration complète
                    UpdatePreviewConfiguration();

                    System.Diagnostics.Debug.WriteLine("PreviewButton_Click: Nouveau gestionnaire de prévisualisation créé");
                }

                // Démarrer ou arrêter la prévisualisation via le gestionnaire global
                if (previewManager.IsPreviewActive)
                {
                    System.Diagnostics.Debug.WriteLine("PreviewButton_Click: Arrêt de la prévisualisation via gestionnaire global");

                    // Mise à jour proactive de l'état des boutons
                    UpdateAllPreviewButtonsState(false);

                    // Vérifier si le timer temporaire est actif et l'arrêter si c'est le cas
                    if (_temporaryPreviewTimer != null && _temporaryPreviewTimer.IsEnabled)
                    {
                        System.Diagnostics.Debug.WriteLine("PreviewButton_Click: Arrêt du timer temporaire");
                        _temporaryPreviewTimer.Stop();
                    }

                    // Arrêter la prévisualisation
                    previewManager.StopPreview();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("PreviewButton_Click: Démarrage de la prévisualisation via gestionnaire global");

                    // Mise à jour de l'état des boutons
                    UpdateAllPreviewButtonsState(true);

                    // Mettre à jour la configuration avant de démarrer la prévisualisation
                    // pour s'assurer que tous les paramètres sont corrects
                    UpdatePreviewConfiguration();

                    // Démarrer la prévisualisation
                    previewManager.StartPreview(new Size(width, height));

                    // Déclencher l'événement PreviewDimensionsRequested pour informer les écouteurs
                    RequestPreviewDimensions(width, height);
                }
            }
            catch (Exception ex)
            {
                // Journaliser et gérer toute exception pour une robustesse maximale
                System.Diagnostics.Debug.WriteLine($"ERREUR dans PreviewButton_Click: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // Tentative de récupération - réinitialiser l'état des boutons par sécurité
                UpdateAllPreviewButtonsState(false);

                // Informer l'utilisateur de manière conviviale
                MessageBox.Show(
                    $"Une erreur est survenue lors de la prévisualisation : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement pour le bouton d'application des dimensions
        /// </summary>
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ApplySettings();
        }

        /// <summary>
        /// Gestionnaire de l'événement pour le bouton de réinitialisation
        /// </summary>
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetSettings();
        }

        /// <summary>
        /// Gestionnaire de l'événement pour le bouton d'application temporaire.
        /// Met en place un mécanisme de protection contre les clics multiples et gère
        /// la prévisualisation temporaire des dimensions en utilisant préférentiellement
        /// le coordinateur central de prévisualisation. Met également à jour l'état
        /// des boutons pour assurer leur cohérence.
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void ApplyTemporaryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Bouton 'Appliquer temporairement' cliqué");

                // Mettre à jour l'état de tous les boutons immédiatement pour empêcher les clics multiples
                UpdateAllPreviewButtonsState(true);

                // Valider et récupérer les dimensions depuis les TextBox
                if (!double.TryParse(WidthTextBox.Text, out double width))
                {
                    System.Diagnostics.Debug.WriteLine($"AVERTISSEMENT: Valeur de largeur invalide: '{WidthTextBox.Text}'");
                    MessageBox.Show("La largeur doit être un nombre valide", "Valeur incorrecte", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // Réactiver les boutons en cas d'erreur de validation
                    UpdateAllPreviewButtonsState(false);
                    return;
                }

                if (!double.TryParse(HeightTextBox.Text, out double height))
                {
                    System.Diagnostics.Debug.WriteLine($"AVERTISSEMENT: Valeur de hauteur invalide: '{HeightTextBox.Text}'");
                    MessageBox.Show("La hauteur doit être un nombre valide", "Valeur incorrecte", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // Réactiver les boutons en cas d'erreur de validation
                    UpdateAllPreviewButtonsState(false);
                    return;
                }

                // Valider que les dimensions sont positives
                if (width <= 0 || height <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"AVERTISSEMENT: Dimensions invalides: {width}x{height}");
                    MessageBox.Show("Les dimensions doivent être positives", "Valeur incorrecte", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // Réactiver les boutons en cas d'erreur de validation
                    UpdateAllPreviewButtonsState(false);
                    return;
                }

                // Récupérer la durée sélectionnée depuis le slider
                double duration = 5.0; // Valeur par défaut
                if (TemporaryDurationSlider != null)
                {
                    duration = TemporaryDurationSlider.Value;
                }

                // Approche 1: Utiliser le coordinateur de prévisualisation si disponible (approche préférée)
                if (_previewCoordinator != null && _previewCoordinator.IsInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("ApplyTemporaryButton_Click: Utilisation du coordinateur de prévisualisation");

                    // Mettre à jour la configuration de prévisualisation
                    UpdatePreviewConfiguration();

                    // Arrêter toute prévisualisation active
                    if (_previewCoordinator.IsPreviewActive)
                    {
                        _previewCoordinator.StopPreview();
                        System.Diagnostics.Debug.WriteLine("Prévisualisation en cours arrêtée proprement via coordinateur");
                    }

                    // Démarrer la prévisualisation temporaire
                    _previewCoordinator.StartPreview(new Size(width, height));
                    System.Diagnostics.Debug.WriteLine($"Prévisualisation temporaire démarrée via coordinateur avec dimensions {width}x{height}");

                    // Configurer et démarrer le timer
                    _temporaryPreviewTimer.Interval = TimeSpan.FromSeconds(duration);
                    _temporaryPreviewTimer.Tick -= TemporaryPreviewTimer_Tick; // Se désabonner d'abord pour éviter les abonnements multiples
                    _temporaryPreviewTimer.Tick += TemporaryPreviewTimer_Tick; // S'abonner à l'événement Tick
                    _temporaryPreviewTimer.Start();
                    System.Diagnostics.Debug.WriteLine($"Timer démarré pour {duration} secondes");

                    // Mettre à jour l'état des boutons de prévisualisation
                    UpdateAllPreviewButtonsState(true);

                    return;
                }

                // Approche 2: Utiliser le gestionnaire global si le coordinateur n'est pas disponible
                // Cette partie reprend la logique existante comme solution de secours

                if (_parentWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERREUR: Fenêtre parente non définie");
                    MessageBox.Show(
                        "Impossible de prévisualiser : la fenêtre parente n'est pas définie.",
                        "Erreur de prévisualisation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    // Réactiver les boutons en cas d'erreur
                    UpdateAllPreviewButtonsState(false);
                    return;
                }

                IWindowPreviewManager previewManager = _parentWindow.GetPreviewManager();
                if (previewManager == null)
                {
                    try
                    {
                        // Obtenir les paramètres actuels
                        var settings = AppSettings.Instance.InterfaceSettings;

                        // Créer un nouveau gestionnaire avec les paramètres actuels
                        previewManager = _parentWindow.EnablePreview(
                            settings.PreviewRendererType,
                            settings.PositionStrategyType);

                        System.Diagnostics.Debug.WriteLine("ApplyTemporaryButton_Click: Nouveau gestionnaire de prévisualisation créé");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ERREUR lors de la création du gestionnaire de prévisualisation: {ex.Message}");

                        // Réactiver les boutons en cas d'erreur
                        UpdateAllPreviewButtonsState(false);
                        return;
                    }
                }

                // Mettre à jour la configuration complète
                UpdatePreviewConfiguration();

                // IMPORTANT: Vérifier si une prévisualisation est déjà active et l'arrêter correctement
                // avant d'en démarrer une nouvelle pour éviter les références nulles
                try
                {
                    if (previewManager.IsPreviewActive)
                    {
                        previewManager.StopPreview();
                        System.Diagnostics.Debug.WriteLine("Prévisualisation en cours arrêtée proprement");

                        // Attendre un court instant pour s'assurer que les ressources sont libérées
                        System.Threading.Thread.Sleep(50); // 50ms de délai pour éviter les conditions de course
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERREUR lors de l'arrêt de la prévisualisation: {ex.Message}");
                    // Continuer malgré l'erreur, mais journaliser pour le débogage
                }

                // Arrondir les dimensions pour une meilleure présentation
                double roundedWidth = Math.Round(width);
                double roundedHeight = Math.Round(height);

                // Démarrer la prévisualisation
                previewManager.StartPreview(new Size(roundedWidth, roundedHeight));
                System.Diagnostics.Debug.WriteLine($"Prévisualisation temporaire démarrée via gestionnaire global avec dimensions {roundedWidth}x{roundedHeight}");

                // Mettre à jour l'état des boutons de prévisualisation
                UpdateAllPreviewButtonsState(true);

                // Configurer et démarrer le timer
                _temporaryPreviewTimer.Interval = TimeSpan.FromSeconds(duration);
                _temporaryPreviewTimer.Tick -= TemporaryPreviewTimer_Tick; // Se désabonner d'abord pour éviter les abonnements multiples
                _temporaryPreviewTimer.Tick += TemporaryPreviewTimer_Tick; // S'abonner à l'événement Tick
                _temporaryPreviewTimer.Start();
                System.Diagnostics.Debug.WriteLine($"Timer démarré pour {duration} secondes");
            }
            catch (Exception ex)
            {
                // Journaliser et gérer toute exception pour une robustesse maximale
                System.Diagnostics.Debug.WriteLine($"ERREUR dans ApplyTemporaryButton_Click: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // Réactiver les boutons en cas d'erreur
                UpdateAllPreviewButtonsState(false);

                // Informer l'utilisateur de manière conviviale
                MessageBox.Show(
                    $"Une erreur est survenue lors de l'application temporaire : {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Réactive le bouton d'application temporaire et restaure son texte d'origine.
        /// Cette méthode est appelée lorsque la prévisualisation temporaire est terminée
        /// ou en cas d'erreur pendant la prévisualisation. Elle utilise maintenant la méthode
        /// centralisée UpdateAllPreviewButtonsState pour garantir la cohérence des états des boutons.
        /// </summary>
        private void ReactivateTemporaryButton()
        {
            try
            {
                // Utiliser la méthode centralisée pour mettre à jour l'état de tous les boutons
                // Cela garantit que le bouton temporaire ainsi que le bouton standard sont dans un état cohérent
                UpdateAllPreviewButtonsState(false);

                // Journaliser l'action
                System.Diagnostics.Debug.WriteLine("Boutons de prévisualisation réactivés via ReactivateTemporaryButton");
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur sans perturber l'application
                System.Diagnostics.Debug.WriteLine($"ERREUR dans ReactivateTemporaryButton: {ex.Message}");

                // Tentative de dernière chance pour réactiver les boutons
                try
                {
                    if (ApplyTemporaryButton != null)
                    {
                        ApplyTemporaryButton.IsEnabled = true;
                        ApplyTemporaryButton.Content = "Appliquer temporairement";
                    }

                    if (PreviewButton != null)
                    {
                        PreviewButton.Content = "Prévisualiser";

                        // Réappliquer le style d'origine
                        if (Resources.Contains("ActionButtonStyle"))
                        {
                            Style originalStyle = Resources["ActionButtonStyle"] as Style;
                            if (originalStyle != null)
                            {
                                PreviewButton.Style = originalStyle;
                                PreviewButton.ClearValue(Button.BackgroundProperty);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignorer l'erreur silencieusement - nous avons fait de notre mieux
                }
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement pour le changement de l'option de prévisualisation en direct
        /// </summary>
        private void LivePreviewCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Ignorer si on est en train de mettre à jour les contrôles
            if (_isUpdatingControls)
                return;

            try
            {
                // Mettre à jour l'état de la prévisualisation en temps réel
                _livePreviewEnabled = LivePreviewCheckBox.IsChecked.GetValueOrDefault(false);

                System.Diagnostics.Debug.WriteLine($"État de prévisualisation en temps réel changé: {_livePreviewEnabled}");

                // Vérifier que la fenêtre parente existe
                if (_parentWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("LivePreviewCheckBox_CheckedChanged: _parentWindow est null");
                    return;
                }

                // Récupérer ou créer le gestionnaire global de prévisualisation
                IWindowPreviewManager previewManager = _parentWindow.GetPreviewManager();
                if (previewManager == null)
                {
                    // Créer un nouveau gestionnaire en utilisant les paramètres actuels
                    var settings = AppSettings.Instance.InterfaceSettings;
                    previewManager = _parentWindow.EnablePreview(
                        settings.PreviewRendererType,
                        settings.PositionStrategyType);

                    System.Diagnostics.Debug.WriteLine("LivePreviewCheckBox_CheckedChanged: Nouveau gestionnaire global créé");
                }

                // Arrêter la prévisualisation existante si elle est active
                if (previewManager.IsPreviewActive)
                {
                    previewManager.StopPreview();
                    System.Diagnostics.Debug.WriteLine("LivePreviewCheckBox_CheckedChanged: Prévisualisation active arrêtée");
                }

                // Mettre à jour la configuration globale du système de prévisualisation
                // via la méthode centralisée (s'assure que le renderer est correctement configuré)
                UpdatePreviewConfiguration();

                // Configurer les gestionnaires d'événements des contrôles de dimensions
                SetupLivePreviewEventHandlers();

                // Démarrer immédiatement la prévisualisation si la case est cochée
                if (_livePreviewEnabled)
                {
                    // Récupérer les dimensions actuelles
                    double width, height;
                    if (double.TryParse(WidthTextBox.Text, out width) &&
                        double.TryParse(HeightTextBox.Text, out height))
                    {
                        // Démarrer la prévisualisation avec les dimensions actuelles
                        previewManager.StartPreview(new Size(width, height));
                        System.Diagnostics.Debug.WriteLine($"LivePreviewCheckBox_CheckedChanged: Prévisualisation démarrée avec dimensions {width}x{height}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur pour le débogage
                System.Diagnostics.Debug.WriteLine($"Erreur lors du changement d'état de prévisualisation en temps réel: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement TextChanged pour le TextBox de largeur
        /// Met à jour le Slider correspondant et vérifie si les dimensions correspondent à un preset
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void WidthTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Ignorer l'événement si on est en train de mettre à jour les contrôles
            if (_isUpdatingControls)
                return;

            // Ajouter une vérification pour s'assurer que WidthSlider n'est pas null
            if (WidthSlider != null && double.TryParse(WidthTextBox.Text, out double width))
            {
                _isUpdatingControls = true;
                // Limiter la valeur du Slider entre son minimum et son maximum
                WidthSlider.Value = Math.Max(WidthSlider.Minimum, Math.Min(WidthSlider.Maximum, width));
                _isUpdatingControls = false;
            }

            // Vérifier si les dimensions actuelles correspondent à un preset
            CheckIfDimensionsMatchPreset();

            // Mettre à jour les informations d'écran et les avertissements
            UpdateScreenInfo();
        }

        /// <summary>
        /// Gestionnaire de l'événement TextChanged pour le TextBox de hauteur
        /// Met à jour le Slider correspondant et vérifie si les dimensions correspondent à un preset
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void HeightTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Ignorer l'événement si on est en train de mettre à jour les contrôles
            if (_isUpdatingControls)
                return;

            // Vérifier que HeightSlider n'est pas null
            if (HeightSlider != null && double.TryParse(HeightTextBox.Text, out double height))
            {
                _isUpdatingControls = true;
                // Limiter la valeur du Slider entre son minimum et son maximum
                HeightSlider.Value = Math.Max(HeightSlider.Minimum, Math.Min(HeightSlider.Maximum, height));
                _isUpdatingControls = false;
            }

            // Vérifier si les dimensions actuelles correspondent à un preset
            CheckIfDimensionsMatchPreset();

            // Mettre à jour les informations d'écran et les avertissements
            UpdateScreenInfo();
        }

        /// <summary>
        /// Gestionnaire de l'événement ValueChanged pour le Slider de largeur
        /// Met à jour le TextBox correspondant et vérifie si les dimensions correspondent à un preset
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Ignorer l'événement si on est en train de mettre à jour les contrôles
            if (_isUpdatingControls)
                return;

            // Mettre à jour le TextBox
            _isUpdatingControls = true;
            WidthTextBox.Text = ((int)WidthSlider.Value).ToString();
            _isUpdatingControls = false;

            // Vérifier si les dimensions actuelles correspondent à un preset
            CheckIfDimensionsMatchPreset();

            // Mettre à jour les informations d'écran et les avertissements
            UpdateScreenInfo();
        }

        /// <summary>
        /// Gestionnaire de l'événement ValueChanged pour le Slider de hauteur
        /// Met à jour le TextBox correspondant et vérifie si les dimensions correspondent à un preset
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Ignorer l'événement si on est en train de mettre à jour les contrôles
            if (_isUpdatingControls)
                return;

            // Mettre à jour le TextBox
            _isUpdatingControls = true;
            HeightTextBox.Text = ((int)HeightSlider.Value).ToString();
            _isUpdatingControls = false;

            // Vérifier si les dimensions actuelles correspondent à un preset
            CheckIfDimensionsMatchPreset();

            // Mettre à jour les informations d'écran et les avertissements
            UpdateScreenInfo();
        }

        /// <summary>
        /// Gestionnaire de l'événement pour limiter les saisies aux chiffres dans les TextBox
        /// </summary>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Gestionnaire de l'événement pour le bouton de taille plein écran
        /// Définit les dimensions de la fenêtre à la taille complète de l'écran principal
        /// en tenant compte du facteur d'échelle DPI.
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obtenir les dimensions du moniteur principal
                var primaryScreen = ScreenUtility.PrimaryMonitor;
                if (primaryScreen != null)
                {
                    // Obtenir le facteur d'échelle DPI
                    double dpiScaleFactor = WindowPositioningHelper.GetDpiScaleFactor(primaryScreen);

                    // Convertir les dimensions physiques de l'écran en dimensions logiques
                    double logicalWidth = primaryScreen.Width / dpiScaleFactor;
                    double logicalHeight = primaryScreen.Height / dpiScaleFactor;

                    // Journaliser les dimensions pour le débogage
                    System.Diagnostics.Debug.WriteLine($"FullScreenButton_Click: Dimensions physiques de l'écran: {primaryScreen.Width}x{primaryScreen.Height}");
                    System.Diagnostics.Debug.WriteLine($"FullScreenButton_Click: Facteur d'échelle DPI: {dpiScaleFactor}");
                    System.Diagnostics.Debug.WriteLine($"FullScreenButton_Click: Dimensions logiques calculées: {logicalWidth}x{logicalHeight}");

                    // Appliquer les dimensions logiques
                    UpdateDimensionControls(logicalWidth, logicalHeight);
                }
                else
                {
                    // Journaliser un avertissement si aucun écran n'est détecté
                    System.Diagnostics.Debug.WriteLine("Avertissement: Impossible de détecter l'écran principal.");
                }
            }
            catch (Exception ex)
            {
                // Journaliser et gérer toute exception pour une robustesse maximale
                System.Diagnostics.Debug.WriteLine($"Erreur lors du redimensionnement en plein écran: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement pour le bouton de demi-écran
        /// Définit les dimensions de la fenêtre à exactement la moitié de la surface de l'écran
        /// tout en conservant le ratio d'aspect original et en tenant compte du facteur d'échelle DPI.
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void HalfScreenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obtenir les dimensions du moniteur principal
                var primaryScreen = ScreenUtility.PrimaryMonitor;
                if (primaryScreen != null)
                {
                    // Obtenir le facteur d'échelle DPI
                    double dpiScaleFactor = WindowPositioningHelper.GetDpiScaleFactor(primaryScreen);

                    // Convertir les dimensions physiques de l'écran en dimensions logiques
                    double logicalWidth = primaryScreen.Width / dpiScaleFactor;
                    double logicalHeight = primaryScreen.Height / dpiScaleFactor;

                    // Calculer les dimensions avec un facteur de surface de 0.5 (moitié)
                    var dimensions = CalculateDimensionsWithSurfaceFactor(
                        logicalWidth,
                        logicalHeight,
                        0.5);

                    // Journaliser les dimensions pour le débogage
                    System.Diagnostics.Debug.WriteLine($"HalfScreenButton_Click: Dimensions physiques de l'écran: {primaryScreen.Width}x{primaryScreen.Height}");
                    System.Diagnostics.Debug.WriteLine($"HalfScreenButton_Click: Facteur d'échelle DPI: {dpiScaleFactor}");
                    System.Diagnostics.Debug.WriteLine($"HalfScreenButton_Click: Dimensions logiques calculées: {dimensions.Item1}x{dimensions.Item2}");

                    // Appliquer les nouvelles dimensions
                    UpdateDimensionControls(dimensions.Item1, dimensions.Item2);
                }
                else
                {
                    // Journaliser un avertissement si aucun écran n'est détecté
                    System.Diagnostics.Debug.WriteLine("Avertissement: Impossible de détecter l'écran principal.");
                }
            }
            catch (Exception ex)
            {
                // Journaliser et gérer toute exception pour une robustesse maximale
                System.Diagnostics.Debug.WriteLine($"Erreur lors du redimensionnement en demi-écran: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement pour le bouton de quart d'écran
        /// Définit les dimensions de la fenêtre à exactement un quart de la surface de l'écran
        /// tout en conservant le ratio d'aspect original et en tenant compte du facteur d'échelle DPI.
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void QuarterScreenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obtenir les dimensions du moniteur principal
                var primaryScreen = ScreenUtility.PrimaryMonitor;
                if (primaryScreen != null)
                {
                    // Obtenir le facteur d'échelle DPI
                    double dpiScaleFactor = WindowPositioningHelper.GetDpiScaleFactor(primaryScreen);

                    // Convertir les dimensions physiques de l'écran en dimensions logiques
                    double logicalWidth = primaryScreen.Width / dpiScaleFactor;
                    double logicalHeight = primaryScreen.Height / dpiScaleFactor;

                    // Calculer les dimensions avec un facteur de surface de 0.25 (quart)
                    var dimensions = CalculateDimensionsWithSurfaceFactor(
                        logicalWidth,
                        logicalHeight,
                        0.25);

                    // Journaliser les dimensions pour le débogage
                    System.Diagnostics.Debug.WriteLine($"QuarterScreenButton_Click: Dimensions physiques de l'écran: {primaryScreen.Width}x{primaryScreen.Height}");
                    System.Diagnostics.Debug.WriteLine($"QuarterScreenButton_Click: Facteur d'échelle DPI: {dpiScaleFactor}");
                    System.Diagnostics.Debug.WriteLine($"QuarterScreenButton_Click: Dimensions logiques calculées: {dimensions.Item1}x{dimensions.Item2}");

                    // Appliquer les nouvelles dimensions
                    UpdateDimensionControls(dimensions.Item1, dimensions.Item2);
                }
                else
                {
                    // Journaliser un avertissement si aucun écran n'est détecté
                    System.Diagnostics.Debug.WriteLine("Avertissement: Impossible de détecter l'écran principal.");
                }
            }
            catch (Exception ex)
            {
                // Journaliser et gérer toute exception pour une robustesse maximale
                System.Diagnostics.Debug.WriteLine($"Erreur lors du redimensionnement en quart d'écran: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement pour le bouton de sauvegarde de position
        /// </summary>
        private void SavePositionButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(WidthTextBox.Text, out double width) &&
                double.TryParse(HeightTextBox.Text, out double height))
            {
                // Demander un nom pour la position
                string positionName = Microsoft.VisualBasic.Interaction.InputBox(
                    "Entrez un nom pour cette position :",
                    "Sauvegarder la position",
                    $"Position {width}x{height}");

                if (!string.IsNullOrEmpty(positionName) && _presetManager != null)
                {
                    // Sauvegarder la position
                    // La méthode SetPreset contient maintenant la sauvegarde dans un fichier
                    _presetManager.SetPreset(positionName, new Size(width, height));

                    // Mettre à jour le ComboBox
                    _presetManager.ConnectToComboBox(SavedPositionsComboBox);

                    // Sélectionner la nouvelle position
                    SavedPositionsComboBox.SelectedItem = positionName;

                    // Afficher une confirmation à l'utilisateur
                    System.Windows.MessageBox.Show(
                        $"La position '{positionName}' a été sauvegardée avec succès et sera disponible au prochain démarrage de l'application.",
                        "Position sauvegardée",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement pour le bouton de suppression de position
        /// </summary>
        private void DeletePositionButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedPreset = SavedPositionsComboBox.SelectedItem as string;
            if (!string.IsNullOrEmpty(selectedPreset) && _presetManager != null)
            {
                // Demander confirmation
                if (MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer la position '{selectedPreset}' ?\nCette position sera définitivement supprimée et ne sera plus disponible au prochain démarrage de l'application.",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    // Supprimer la position
                    // La méthode RemovePreset contient maintenant la mise à jour du fichier
                    _presetManager.RemovePreset(selectedPreset);

                    // Mettre à jour le ComboBox
                    _presetManager.ConnectToComboBox(SavedPositionsComboBox);

                    // Afficher une confirmation à l'utilisateur
                    System.Windows.MessageBox.Show(
                        $"La position '{selectedPreset}' a été supprimée avec succès.",
                        "Position supprimée",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
        }

        #endregion

        #region Méthodes utilitaires

        /// <summary>
        /// Met à jour l'état de tous les boutons liés à la prévisualisation pour assurer leur cohérence.
        /// Cette méthode centralise la gestion des états visuels des boutons pour garantir qu'ils
        /// reflètent correctement l'état actuel de la prévisualisation, évitant ainsi les incohérences
        /// et les conflits d'état.
        /// </summary>
        /// <param name="isPreviewActive">Indique si une prévisualisation est active (true) ou inactive (false)</param>
        private void UpdateAllPreviewButtonsState(bool isPreviewActive)
        {
            try
            {
                // Protection contre les appels récursifs qui pourraient créer une boucle infinie
                if (_isUpdatingPreviewButton)
                {
                    System.Diagnostics.Debug.WriteLine("UpdateAllPreviewButtonsState: Évitement d'appel récursif");
                    return;
                }

                _isUpdatingPreviewButton = true;

                try
                {
                    // Vérification que les boutons existent avant de tenter de les modifier
                    if (PreviewButton == null)
                    {
                        System.Diagnostics.Debug.WriteLine("UpdateAllPreviewButtonsState: PreviewButton est null");
                        // Ne pas sortir ici, car on peut vouloir mettre à jour uniquement ApplyTemporaryButton
                    }

                    if (ApplyTemporaryButton == null)
                    {
                        System.Diagnostics.Debug.WriteLine("UpdateAllPreviewButtonsState: ApplyTemporaryButton est null");
                        // Ne pas sortir ici, car on peut vouloir mettre à jour uniquement PreviewButton
                    }

                    // Mise à jour synchronisée du bouton "Prévisualiser"
                    if (PreviewButton != null)
                    {
                        // Mise à jour du texte du bouton en fonction de l'état de la prévisualisation
                        if (isPreviewActive)
                        {
                            // La prévisualisation est active, le bouton doit permettre de l'arrêter
                            PreviewButton.Content = "Arrêter la prévisualisation";

                            // Appliquer un fond rouge pour attirer l'attention sur le fait que le bouton arrête la prévisualisation
                            PreviewButton.Background = new System.Windows.Media.SolidColorBrush(
                                System.Windows.Media.Color.FromRgb(231, 76, 60)); // Rouge plus doux
                        }
                        else
                        {
                            // La prévisualisation est inactive, le bouton doit permettre de la démarrer
                            PreviewButton.Content = "Prévisualiser";

                            // Réappliquer le style d'origine pour restaurer l'apparence standard du bouton
                            if (Resources.Contains("ActionButtonStyle"))
                            {
                                Style originalStyle = Resources["ActionButtonStyle"] as Style;
                                if (originalStyle != null)
                                {
                                    PreviewButton.Style = originalStyle;

                                    // Réinitialiser explicitement le Background à null pour que le style soit appliqué correctement
                                    PreviewButton.ClearValue(Button.BackgroundProperty);
                                }
                            }
                        }
                    }

                    // Mise à jour synchronisée du bouton "Appliquer temporairement"
                    if (ApplyTemporaryButton != null)
                    {
                        if (isPreviewActive)
                        {
                            // Si une prévisualisation est active, désactiver le bouton pour éviter les conflits
                            ApplyTemporaryButton.IsEnabled = false;
                            ApplyTemporaryButton.Content = "Prévisualisation en cours...";
                        }
                        else
                        {
                            // Réactiver le bouton et restaurer son texte d'origine
                            ApplyTemporaryButton.IsEnabled = true;
                            ApplyTemporaryButton.Content = "Appliquer temporairement";
                        }
                    }

                    // Journaliser l'état pour faciliter le débogage
                    System.Diagnostics.Debug.WriteLine($"État des boutons de prévisualisation mis à jour: {(isPreviewActive ? "actif" : "inactif")}");
                }
                finally
                {
                    // S'assurer que le drapeau est toujours réinitialisé, même en cas d'exception
                    _isUpdatingPreviewButton = false;
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur sans perturber l'application
                System.Diagnostics.Debug.WriteLine($"ERREUR dans UpdateAllPreviewButtonsState: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // Tentative de récupération d'urgence en cas d'erreur
                try
                {
                    if (PreviewButton != null)
                    {
                        // Réinitialiser le bouton à un état connu en cas d'erreur
                        PreviewButton.Content = isPreviewActive ? "Arrêter la prévisualisation" : "Prévisualiser";
                    }

                    if (ApplyTemporaryButton != null)
                    {
                        // Réinitialiser le bouton à un état connu en cas d'erreur
                        ApplyTemporaryButton.IsEnabled = !isPreviewActive;
                        ApplyTemporaryButton.Content = isPreviewActive ? "Prévisualisation en cours..." : "Appliquer temporairement";
                    }
                }
                catch
                {
                    // Ignorer toute erreur pendant la récupération d'urgence
                }

                // Réinitialiser le drapeau en cas d'erreur
                _isUpdatingPreviewButton = false;
            }
        }

        /// <summary>
        /// Met à jour l'état visuel du bouton de prévisualisation pour refléter l'état actif ou inactif
        /// de la prévisualisation. Cette méthode est conservée pour la compatibilité avec le code existant
        /// mais utilise maintenant la méthode centrale UpdateAllPreviewButtonsState.
        /// </summary>
        /// <param name="isPreviewActive">Indique si la prévisualisation est active (true) ou inactive (false)</param>
        private void UpdatePreviewButtonState(bool isPreviewActive)
        {
            // Déléguer à la méthode centralisée qui met à jour tous les boutons de prévisualisation
            UpdateAllPreviewButtonsState(isPreviewActive);
        }

        /// <summary>
        /// Nettoie les ressources associées au système de prévisualisation
        /// et garantit qu'un seul gestionnaire est utilisé.
        /// </summary>
        private void CleanupPreviewAdapter()
        {
            try
            {
                // Arrêter toute prévisualisation active via le gestionnaire global
                if (_parentWindow != null)
                {
                    IWindowPreviewManager previewManager = _parentWindow.GetPreviewManager();
                    if (previewManager != null && previewManager.IsPreviewActive)
                    {
                        previewManager.StopPreview();
                        System.Diagnostics.Debug.WriteLine("Prévisualisation active arrêtée");
                    }
                }

                // Supprimer toute référence à l'ancien adaptateur qui pourrait rester dans TagProperty
                this.SetValue(FrameworkElement.TagProperty, null);
                System.Diagnostics.Debug.WriteLine("Adaptateur de prévisualisation nettoyé");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR dans CleanupPreviewAdapter: {ex.Message}");
            }
        }

        /// <summary>
        /// Met à jour l'interface utilisateur pour indiquer que la fenêtre est maximisée
        /// Cela ajoute une indication visuelle pour informer l'utilisateur que les dimensions
        /// affichées sont celles de la fenêtre maximisée et non celles qui seront appliquées
        /// lorsque la fenêtre sera restaurée.
        /// </summary>
        private void UpdateMaximizedIndicator()
        {
            // Vérifier si la fenêtre parente est maximisée
            bool isParentMaximized = (_parentWindow != null && _parentWindow.WindowState == WindowState.Maximized);

            // Si DimensionWarningText n'existe pas, sortir de la méthode
            if (DimensionWarningText == null)
                return;

            if (isParentMaximized)
            {
                // Afficher un message spécifique pour l'état maximisé
                DimensionWarningText.Text = "Fenêtre maximisée: les dimensions affichées sont celles de la fenêtre actuelle.";
                DimensionWarningText.Visibility = Visibility.Visible;
                DimensionWarningText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(50, 130, 230)); // Bleu informatif au lieu du rouge d'avertissement
            }
            else
            {
                // Restaurer le comportement normal du texte d'avertissement
                DimensionWarningText.Text = "Les dimensions dépassent les limites de l'écran.";
                DimensionWarningText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(229, 57, 53)); // Couleur d'avertissement rouge d'origine

                // L'affichage du texte est géré par UpdateScreenInfo() qui a déjà été appelé
            }
        }

        /// <summary>
        /// Méthode utilitaire pour calculer les dimensions en fonction d'un facteur de surface
        /// tout en conservant le ratio d'aspect. Cette méthode travaille directement
        /// avec des dimensions logiques (déjà converties depuis les dimensions physiques).
        /// </summary>
        /// <param name="originalWidth">Largeur originale (en unités logiques)</param>
        /// <param name="originalHeight">Hauteur originale (en unités logiques)</param>
        /// <param name="surfaceFactor">Facteur de surface (0.5 pour moitié, 0.25 pour quart)</param>
        /// <param name="minDimension">Dimension minimale à respecter (valeur par défaut: 300)</param>
        /// <returns>Tuple contenant la nouvelle largeur et hauteur en unités logiques</returns>
        private Tuple<int, int> CalculateDimensionsWithSurfaceFactor(
            double originalWidth,
            double originalHeight,
            double surfaceFactor,
            int minDimension = 300)
        {
            // Vérifier les paramètres d'entrée
            if (originalWidth <= 0 || originalHeight <= 0)
            {
                throw new ArgumentException("Les dimensions originales doivent être positives");
            }

            if (surfaceFactor <= 0 || surfaceFactor > 1)
            {
                throw new ArgumentException("Le facteur de surface doit être compris entre 0 (exclus) et 1 (inclus)");
            }

            // Journaliser les paramètres d'entrée pour le débogage
            System.Diagnostics.Debug.WriteLine($"CalculateDimensionsWithSurfaceFactor: Dimensions originales: {originalWidth}x{originalHeight}, Facteur: {surfaceFactor}");

            // Calculer le facteur d'échelle en prenant la racine carrée du facteur de surface
            // Ce calcul maintient le ratio d'aspect tout en réduisant la surface selon le facteur
            double scaleFactor = Math.Sqrt(surfaceFactor);

            // Calculer les nouvelles dimensions
            int newWidth = (int)Math.Round(originalWidth * scaleFactor);
            int newHeight = (int)Math.Round(originalHeight * scaleFactor);

            // Appliquer la dimension minimale si nécessaire
            newWidth = Math.Max(minDimension, newWidth);
            newHeight = Math.Max(minDimension, newHeight);

            // Journaliser les dimensions calculées pour le débogage
            System.Diagnostics.Debug.WriteLine($"CalculateDimensionsWithSurfaceFactor: Dimensions calculées: {newWidth}x{newHeight}");

            // Retourner les nouvelles dimensions
            return new Tuple<int, int>(newWidth, newHeight);
        }

        /// <summary>
        /// Vérifie si les dimensions actuelles correspondent à un preset et met à jour la sélection du ComboBox
        /// Cette méthode permet une liaison bidirectionnelle intelligente entre les contrôles
        /// en tenant compte du facteur d'échelle DPI.
        /// </summary>
        private void CheckIfDimensionsMatchPreset()
        {
            // Ignorer si on est en train de mettre à jour les contrôles ou si le gestionnaire n'est pas initialisé
            if (_isUpdatingControls || _presetManager == null)
                return;

            try
            {
                // Éviter les mises à jour récursives pendant la vérification
                _isUpdatingControls = true;

                // Récupérer les dimensions actuelles des TextBox
                if (!double.TryParse(WidthTextBox.Text, out double width) ||
                    !double.TryParse(HeightTextBox.Text, out double height))
                    return;

                // Journaliser les dimensions actuelles pour le débogage
                System.Diagnostics.Debug.WriteLine($"CheckIfDimensionsMatchPreset: Dimensions actuelles: {width}x{height}");

                // Créer un objet Size avec les dimensions actuelles pour faciliter les comparaisons
                Size currentSize = new Size(width, height);

                // Vérifier si les dimensions correspondent à un preset existant
                bool foundMatch = false;

                // Parcourir tous les presets disponibles pour trouver une correspondance
                foreach (string presetName in _presetManager.GetPresetNames())
                {
                    // Obtenir les dimensions du preset courant
                    Size presetSize = _presetManager.GetPreset(presetName);

                    // Utiliser une tolérance de 1 pixel pour la comparaison afin de gérer les erreurs d'arrondi
                    // Cela permet une correspondance même si les dimensions diffèrent légèrement
                    if (Math.Abs(presetSize.Width - currentSize.Width) < 1 &&
                        Math.Abs(presetSize.Height - currentSize.Height) < 1)
                    {
                        // Si les dimensions correspondent, sélectionner ce preset dans le ComboBox
                        if (SavedPositionsComboBox.Items.Contains(presetName))
                        {
                            SavedPositionsComboBox.SelectedItem = presetName;
                            foundMatch = true;
                            System.Diagnostics.Debug.WriteLine($"CheckIfDimensionsMatchPreset: Match trouvé pour les dimensions {width}x{height}: {presetName}");
                            break; // Sortir de la boucle dès qu'une correspondance est trouvée
                        }
                    }
                }

                // Si aucune correspondance n'est trouvée, désélectionner complètement le ComboBox
                if (!foundMatch)
                {
                    System.Diagnostics.Debug.WriteLine($"CheckIfDimensionsMatchPreset: Aucun match trouvé pour les dimensions {width}x{height}");
                    SavedPositionsComboBox.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la vérification des dimensions: {ex.Message}");
            }
            finally
            {
                // Toujours réactiver les mises à jour des contrôles à la fin du traitement
                _isUpdatingControls = false;
            }
        }

        /// <summary>
        /// Initialise les plages des sliders en fonction des dimensions de l'écran principal
        /// et prend en compte le facteur d'échelle DPI.
        /// </summary>
        private void InitializeSliderRanges()
        {
            try
            {
                // Rafraîchir les limites d'écran
                if (_screenLimits == null)
                {
                    _screenLimits = new ScreenLimits();
                }
                else
                {
                    _screenLimits.RefreshLimits();
                }

                // Appliquer les limites aux sliders
                _screenLimits.ApplyToSliders(WidthSlider, HeightSlider);

                // Journaliser les limites des sliders après initialisation
                if (WidthSlider != null && HeightSlider != null)
                {
                    System.Diagnostics.Debug.WriteLine($"InitializeSliderRanges: Slider Largeur - Min={WidthSlider.Minimum}, Max={WidthSlider.Maximum}");
                    System.Diagnostics.Debug.WriteLine($"InitializeSliderRanges: Slider Hauteur - Min={HeightSlider.Minimum}, Max={HeightSlider.Maximum}");
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'initialisation des plages des sliders: {ex.Message}");

                // En cas d'erreur, utiliser des valeurs par défaut
                if (WidthSlider != null)
                {
                    WidthSlider.Maximum = 3000;
                    WidthSlider.TickFrequency = 200;
                }

                if (HeightSlider != null)
                {
                    HeightSlider.Maximum = 2000;
                    HeightSlider.TickFrequency = 150;
                }
            }
        }

        /// <summary>
        /// Met à jour les contrôles de dimensions
        /// </summary>
        /// <param name="width">Nouvelle largeur</param>
        /// <param name="height">Nouvelle hauteur</param>

        /// <summary>
        /// Met à jour les contrôles de dimensions en tenant compte du facteur d'échelle DPI
        /// </summary>
        /// <param name="width">Nouvelle largeur (en unités logiques)</param>
        /// <param name="height">Nouvelle hauteur (en unités logiques)</param>
        private void UpdateDimensionControls(double width, double height)
        {
            // Éviter les mises à jour récursives
            _isUpdatingControls = true;

            try
            {
                // Journaliser les dimensions reçues pour le débogage
                System.Diagnostics.Debug.WriteLine($"UpdateDimensionControls: Dimensions logiques reçues: {width}x{height}");

                // Mettre à jour les TextBox (vérifier qu'ils ne sont pas null)
                if (WidthTextBox != null)
                    WidthTextBox.Text = ((int)width).ToString();

                if (HeightTextBox != null)
                    HeightTextBox.Text = ((int)height).ToString();

                // Mettre à jour les Slider (vérifier qu'ils ne sont pas null)
                if (WidthSlider != null)
                {
                    // Limiter la valeur du slider entre son minimum et son maximum
                    WidthSlider.Value = Math.Max(WidthSlider.Minimum, Math.Min(WidthSlider.Maximum, width));

                    // Journaliser la valeur appliquée au slider
                    System.Diagnostics.Debug.WriteLine($"UpdateDimensionControls: Valeur appliquée au slider de largeur: {WidthSlider.Value}");
                }

                if (HeightSlider != null)
                {
                    // Limiter la valeur du slider entre son minimum et son maximum
                    HeightSlider.Value = Math.Max(HeightSlider.Minimum, Math.Min(HeightSlider.Maximum, height));

                    // Journaliser la valeur appliquée au slider
                    System.Diagnostics.Debug.WriteLine($"UpdateDimensionControls: Valeur appliquée au slider de hauteur: {HeightSlider.Value}");
                }

                // Appliquer les dimensions à la fenêtre parente UNIQUEMENT si explicitement demandé
                // Cette modification permet d'éviter d'appliquer les dimensions lors du chargement initial
                if (_applyDimensionsOnLoad && _parentWindow != null)
                {
                    // Il est important de vérifier que _parentWindow n'est pas null par sécurité
                    if (_parentWindow.WindowState == WindowState.Maximized)
                    {
                        // Si la fenêtre est maximisée, ne pas appliquer les dimensions
                        // car cela restaurerait la fenêtre involontairement
                        System.Diagnostics.Debug.WriteLine("Fenêtre maximisée : dimensions non appliquées");
                    }
                    else
                    {
                        // Appliquer les dimensions uniquement si la fenêtre n'est pas maximisée
                        // Les dimensions sont déjà en unités logiques, donc pas besoin de conversion
                        _parentWindow.Width = width;
                        _parentWindow.Height = height;

                        // Journaliser l'application des dimensions à la fenêtre parente
                        System.Diagnostics.Debug.WriteLine($"UpdateDimensionControls: Dimensions appliquées à la fenêtre parente: {width}x{height}");
                    }
                }

                // Mettre à jour l'interface pour afficher les avertissements si nécessaire
                UpdateScreenInfo();
            }
            finally
            {
                _isUpdatingControls = false;
            }
        }

        // ==================== MODIFIER LA MÉTHODE SettingsPanel_DimensionsChanged ====================
        // (Dans la région #region Gestion du panneau latéral dans MainWindow.xaml.cs)
        // Ceci est une référence, mais sera implémenté dans MainWindow.xaml.cs
        /*
        private void SettingsPanel_DimensionsChanged(object sender, InterfaceSettingsTab.DimensionsChangedEventArgs e)
        {
            // Supprimer d'abord l'aperçu s'il existe
            if (_previewBorder != null)
            {
                RemovePreviewBorder();
            }

            // Vérifier si la fenêtre est maximisée
            if (this.WindowState != WindowState.Maximized)
            {
                // Appliquer les nouvelles dimensions uniquement si la fenêtre n'est pas maximisée
                this.Width = e.Width;
                this.Height = e.Height;
            }
            else
            {
                // Optionnel : Informer l'utilisateur que les dimensions ne seront pas appliquées
                // car la fenêtre est maximisée
                System.Diagnostics.Debug.WriteLine("Fenêtre maximisée : les nouvelles dimensions seront appliquées lors de la restauration");
            }
        }
        */

        /// <summary>
        /// Met à jour les informations d'écran et vérifie si les dimensions sont valides
        /// </summary>

        /// <summary>
        /// Met à jour les informations d'écran et vérifie si les dimensions sont valides
        /// en tenant compte du facteur d'échelle DPI.
        /// </summary>
        private void UpdateScreenInfo()
        {
            if (DimensionWarningText == null)
                return;

            // Vérifier si les valeurs actuelles sont valides
            if (WidthTextBox != null && HeightTextBox != null &&
                double.TryParse(WidthTextBox.Text, out double width) &&
                double.TryParse(HeightTextBox.Text, out double height))
            {
                // Vérifier si les dimensions dépassent les limites de l'écran
                bool showWarning = false;

                // Vérifier directement si les dimensions dépassent les limites
                var primaryScreen = ScreenUtility.PrimaryMonitor;
                if (primaryScreen != null)
                {
                    // Obtenir le facteur d'échelle DPI
                    double dpiScaleFactor = WindowPositioningHelper.GetDpiScaleFactor(primaryScreen);

                    // Convertir les dimensions logiques en dimensions physiques pour la comparaison
                    double physicalWidth = width * dpiScaleFactor;
                    double physicalHeight = height * dpiScaleFactor;

                    // Comparer les dimensions physiques avec les dimensions de l'écran
                    showWarning = (physicalWidth > primaryScreen.Width || physicalHeight > primaryScreen.Height);

                    // Journaliser pour le débogage
                    System.Diagnostics.Debug.WriteLine($"UpdateScreenInfo: Dimensions logiques: {width}x{height}");
                    System.Diagnostics.Debug.WriteLine($"UpdateScreenInfo: Dimensions physiques: {physicalWidth}x{physicalHeight}");
                    System.Diagnostics.Debug.WriteLine($"UpdateScreenInfo: Dimensions écran: {primaryScreen.Width}x{primaryScreen.Height}");
                    System.Diagnostics.Debug.WriteLine($"UpdateScreenInfo: Afficher avertissement: {showWarning}");
                }

                // Afficher ou masquer l'avertissement
                DimensionWarningText.Visibility = showWarning ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                // Masquer l'avertissement si les valeurs ne sont pas valides
                DimensionWarningText.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Méthode appelée lorsque les dimensions de la fenêtre sont modifiées
        /// </summary>
        /// <param name="newWidth">Nouvelle largeur</param>
        /// <param name="newHeight">Nouvelle hauteur</param>
        private void ApplyWindowDimensions(double newWidth, double newHeight)
        {
            // Si la fenêtre parente est disponible
            if (_parentWindow != null)
            {
                // Appliquer les nouvelles dimensions à la fenêtre parente
                _parentWindow.Width = newWidth;
                _parentWindow.Height = newHeight;
            }
        }

        /// <summary>
        /// Crée un fichier "À propos" de test avec des données de démonstration
        /// Note: Utilisé uniquement pour les tests pendant le développement
        /// </summary>
        private void CreateTestAboutFile()
        {
            try
            {
                // Récupérer l'instance des paramètres d'interface
                var interfaceSettings = AppSettings.Instance.InterfaceSettings;

                // Obtenir le chemin du fichier
                string filePath = interfaceSettings.AboutData.AboutFilePath;

                // Données de test
                string[] lines = new string[]
                {
                    "Hello World - Version 1.0",
                    "Application de démonstration",
                    "© 2025 - Tous droits réservés"
                };

                // Écrire dans le fichier
                System.IO.File.WriteAllLines(filePath, lines, System.Text.Encoding.UTF8);

                // Recharger les données
                RefreshAboutData();

                // Afficher un message de confirmation
                MessageBox.Show($"Fichier de test créé avec succès à l'emplacement:\n{filePath}",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la création du fichier de test : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Méthodes de conversion DPI

        /// <summary>
        /// Obtient le facteur d'échelle DPI pour l'écran qui contient la fenêtre parente
        /// </summary>
        /// <returns>Facteur d'échelle DPI</returns>
        private double GetCurrentDpiScaleFactor()
        {
            try
            {
                // Si la fenêtre parente est définie, utiliser son moniteur
                if (_parentWindow != null)
                {
                    var monitor = WindowPositioningHelper.FindMonitorContainingWindow(_parentWindow);
                    if (monitor != null)
                    {
                        return WindowPositioningHelper.GetDpiScaleFactor(monitor);
                    }
                }

                // Sinon, utiliser le moniteur principal
                var primaryMonitor = ScreenUtility.PrimaryMonitor;
                if (primaryMonitor != null)
                {
                    return WindowPositioningHelper.GetDpiScaleFactor(primaryMonitor);
                }

                // Valeur par défaut si aucun moniteur n'est trouvé
                return 1.0;
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération du facteur d'échelle DPI: {ex.Message}");

                // Valeur par défaut en cas d'erreur
                return 1.0;
            }
        }

        /// <summary>
        /// Convertit des dimensions logiques en dimensions physiques
        /// </summary>
        /// <param name="logicalWidth">Largeur en coordonnées logiques</param>
        /// <param name="logicalHeight">Hauteur en coordonnées logiques</param>
        /// <returns>Dimensions en coordonnées physiques</returns>
        private Size LogicalToPhysical(double logicalWidth, double logicalHeight)
        {
            double dpiScaleFactor = GetCurrentDpiScaleFactor();

            return new Size(
                logicalWidth * dpiScaleFactor,
                logicalHeight * dpiScaleFactor);
        }

        /// <summary>
        /// Convertit des dimensions physiques en dimensions logiques
        /// </summary>
        /// <param name="physicalWidth">Largeur en coordonnées physiques</param>
        /// <param name="physicalHeight">Hauteur en coordonnées physiques</param>
        /// <returns>Dimensions en coordonnées logiques</returns>
        private Size PhysicalToLogical(double physicalWidth, double physicalHeight)
        {
            double dpiScaleFactor = GetCurrentDpiScaleFactor();

            // Vérifier que le facteur d'échelle DPI n'est pas nul ou négatif
            if (dpiScaleFactor <= 0.0)
                return new Size(physicalWidth, physicalHeight);

            return new Size(
                physicalWidth / dpiScaleFactor,
                physicalHeight / dpiScaleFactor);
        }

        #endregion

        #region Classe ScreenLimits pour la gestion des limites d'écran

        /// <summary>
        /// Classe interne pour stocker et calculer les limites d'écran pour la validation des dimensions
        /// Cette classe permet une centralisation des valeurs limites et une meilleure gestion des contraintes
        /// </summary>

        /// <summary>
        /// Classe interne pour stocker et calculer les limites d'écran pour la validation des dimensions
        /// Cette classe permet une centralisation des valeurs limites et une meilleure gestion des contraintes
        /// en tenant compte du facteur d'échelle DPI de l'écran.
        /// </summary>
        private class ScreenLimits
        {
            #region Propriétés des limites d'écran

            /// <summary>
            /// Largeur minimale autorisée pour la fenêtre
            /// </summary>
            public double MinWidth { get; private set; } = 300;

            /// <summary>
            /// Hauteur minimale autorisée pour la fenêtre
            /// </summary>
            public double MinHeight { get; private set; } = 300;

            /// <summary>
            /// Largeur maximale autorisée pour la fenêtre
            /// </summary>
            public double MaxWidth { get; private set; } = 3000;

            /// <summary>
            /// Hauteur maximale autorisée pour la fenêtre
            /// </summary>
            public double MaxHeight { get; private set; } = 2000;

            /// <summary>
            /// Facteur de sécurité pour limiter la taille maximale des fenêtres (pourcentage de l'écran)
            /// </summary>
            public double SafetyFactor { get; set; } = 0.95;

            /// <summary>
            /// Indique si les calculs doivent prendre en compte tous les écrans ou seulement l'écran principal
            /// </summary>
            public bool ConsiderAllScreens { get; set; } = true;

            /// <summary>
            /// Facteur d'échelle DPI du moniteur actuel
            /// </summary>
            private double _currentDpiScaleFactor = 1.0;

            #endregion

            /// <summary>
            /// Initialise une nouvelle instance de la classe ScreenLimits
            /// </summary>
            public ScreenLimits()
            {
                RefreshLimits();
            }

            /// <summary>
            /// Recalcule les limites d'écran en fonction des écrans disponibles
            /// et prend en compte le facteur d'échelle DPI pour convertir les dimensions physiques en dimensions logiques.
            /// </summary>
            /// <returns>True si le recalcul a réussi, sinon False</returns>
            public bool RefreshLimits()
            {
                try
                {
                    // Obtenir les informations sur les écrans
                    var primaryMonitor = ScreenUtility.PrimaryMonitor;

                    if (primaryMonitor != null)
                    {
                        // Obtenir le facteur d'échelle DPI pour l'écran principal
                        _currentDpiScaleFactor = WindowPositioningHelper.GetDpiScaleFactor(primaryMonitor);

                        // Journaliser le facteur d'échelle DPI détecté
                        System.Diagnostics.Debug.WriteLine($"ScreenLimits: Facteur d'échelle DPI: {_currentDpiScaleFactor:F2}");

                        // Utiliser les dimensions physiques de l'écran comme base
                        double physicalMaxWidth = primaryMonitor.Width;
                        double physicalMaxHeight = primaryMonitor.Height;

                        // Pour les configurations multi-écrans, on peut opter pour des limites plus grandes
                        if (ConsiderAllScreens && ScreenUtility.Monitors.Count > 1)
                        {
                            // Utiliser la largeur et la hauteur maximales parmi tous les écrans
                            physicalMaxWidth = ScreenUtility.Monitors.Max(m => m.Width);
                            physicalMaxHeight = ScreenUtility.Monitors.Max(m => m.Height);
                        }

                        // Appliquer le facteur de sécurité pour éviter des fenêtres trop grandes
                        // (on laisse une marge par rapport aux dimensions de l'écran)
                        physicalMaxWidth = physicalMaxWidth * SafetyFactor;
                        physicalMaxHeight = physicalMaxHeight * SafetyFactor;

                        // IMPORTANT: Convertir les dimensions physiques en dimensions logiques
                        // en utilisant la même approche que SmartPositionStrategy
                        MaxWidth = physicalMaxWidth / _currentDpiScaleFactor;
                        MaxHeight = physicalMaxHeight / _currentDpiScaleFactor;

                        // Journaliser les dimensions maximales
                        System.Diagnostics.Debug.WriteLine($"ScreenLimits: Dimensions physiques max: {physicalMaxWidth}x{physicalMaxHeight}");
                        System.Diagnostics.Debug.WriteLine($"ScreenLimits: Dimensions logiques max: {MaxWidth}x{MaxHeight}");

                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    // Journaliser l'erreur
                    System.Diagnostics.Debug.WriteLine($"Erreur lors du calcul des limites d'écran: {ex.Message}");

                    // En cas d'erreur, conserver les valeurs par défaut
                    return false;
                }
            }

            /// <summary>
            /// Valide si les dimensions spécifiées respectent les limites d'écran
            /// </summary>
            /// <param name="width">Largeur à valider (en unités logiques)</param>
            /// <param name="height">Hauteur à valider (en unités logiques)</param>
            /// <param name="errorMessage">Message d'erreur en cas d'échec</param>
            /// <returns>True si les dimensions sont valides, sinon False</returns>
            public bool ValidateDimensions(double width, double height, out string errorMessage)
            {
                errorMessage = string.Empty;

                // Vérifier les limites minimales
                if (width < MinWidth)
                {
                    errorMessage = $"La largeur doit être au moins {MinWidth} pixels.";
                    return false;
                }

                if (height < MinHeight)
                {
                    errorMessage = $"La hauteur doit être au moins {MinHeight} pixels.";
                    return false;
                }

                // IMPORTANT: Prendre en compte le facteur d'échelle DPI pour la validation des limites maximales
                double maxWidthLogical = MaxWidth;
                double maxHeightLogical = MaxHeight;

                // Vérifier les limites maximales
                if (width > maxWidthLogical)
                {
                    errorMessage = $"La largeur ne peut pas dépasser {maxWidthLogical:F0} pixels (limites de l'écran).";
                    return false;
                }

                if (height > maxHeightLogical)
                {
                    errorMessage = $"La hauteur ne peut pas dépasser {maxHeightLogical:F0} pixels (limites de l'écran).";
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Applique les limites d'écran aux sliders spécifiés en tenant compte du facteur d'échelle DPI
            /// </summary>
            /// <param name="widthSlider">Slider pour la largeur</param>
            /// <param name="heightSlider">Slider pour la hauteur</param>
            public void ApplyToSliders(System.Windows.Controls.Slider widthSlider, System.Windows.Controls.Slider heightSlider)
            {
                if (widthSlider != null)
                {
                    widthSlider.Minimum = MinWidth;
                    // Appliquer les limites logiques (converties du physique) aux sliders
                    widthSlider.Maximum = MaxWidth;
                    widthSlider.TickFrequency = MaxWidth / 15;

                    // Journaliser les limites appliquées au slider de largeur
                    System.Diagnostics.Debug.WriteLine($"ScreenLimits: Limites du slider de largeur: Min={widthSlider.Minimum}, Max={widthSlider.Maximum}");
                }

                if (heightSlider != null)
                {
                    heightSlider.Minimum = MinHeight;
                    // Appliquer les limites logiques (converties du physique) aux sliders
                    heightSlider.Maximum = MaxHeight;
                    heightSlider.TickFrequency = MaxHeight / 15;

                    // Journaliser les limites appliquées au slider de hauteur
                    System.Diagnostics.Debug.WriteLine($"ScreenLimits: Limites du slider de hauteur: Min={heightSlider.Minimum}, Max={heightSlider.Maximum}");
                }
            }

            /// <summary>
            /// Convertit des dimensions logiques en dimensions physiques
            /// </summary>
            /// <param name="logicalSize">Dimensions en coordonnées logiques</param>
            /// <returns>Dimensions en coordonnées physiques</returns>
            public Size LogicalToPhysical(Size logicalSize)
            {
                // Appliquer le facteur d'échelle DPI
                return new Size(
                    logicalSize.Width * _currentDpiScaleFactor,
                    logicalSize.Height * _currentDpiScaleFactor);
            }

            /// <summary>
            /// Convertit des dimensions physiques en dimensions logiques
            /// </summary>
            /// <param name="physicalSize">Dimensions en coordonnées physiques</param>
            /// <returns>Dimensions en coordonnées logiques</returns>
            public Size PhysicalToLogical(Size physicalSize)
            {
                // Vérifier que le facteur d'échelle DPI n'est pas nul ou négatif
                if (_currentDpiScaleFactor <= 0.0)
                    return physicalSize;

                // Appliquer l'inverse du facteur d'échelle DPI
                return new Size(
                    physicalSize.Width / _currentDpiScaleFactor,
                    physicalSize.Height / _currentDpiScaleFactor);
            }
        }

        #endregion
    }
}