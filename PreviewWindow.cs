using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Media.Effects;
using HelloWorld;

namespace HelloWorld.Preview
{
    /// <summary>
    /// Fenêtre de prévisualisation légère qui affiche une représentation visuelle 
    /// des nouvelles dimensions de la fenêtre cible.
    /// Implémente un design pattern MVVM et utilise un système de rendu configurable.
    /// </summary>
    public class PreviewWindow : Window
    {
        #region Constantes et membres privés

        // Constantes pour l'API Windows
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        // Ajouter une constante pour gérer le scaling DPI
        private const double DEFAULT_DPI = 96.0;

        // Ajouter un membre pour stocker le facteur d'échelle DPI du moniteur courant
        private double _currentDpiScaleFactor = 1.0;

        // Référence à la fenêtre cible dont on prévisualise les dimensions
        private Window _targetWindow;

        // Renderer utilisé pour créer la représentation visuelle
        private IPreviewRenderer _renderer;

        // Conteneur pour le contenu visuel de la prévisualisation
        private ContentControl _contentHost;

        // ViewModel associé à cette fenêtre
        private PreviewWindowViewModel _viewModel;

        // État et position de déplacement
        private bool _isDragging;
        private Point _dragStartPosition;

        #endregion

        #region Propriétés publiques

        /// <summary>
        /// Obtient ou définit une valeur indiquant si la fenêtre est déplaçable par l'utilisateur
        /// </summary>
        public bool IsDraggable
        {
            get { return (_viewModel != null) ? _viewModel.IsDraggable : true; }
            set
            {
                if (_viewModel != null)
                    _viewModel.IsDraggable = value;
            }
        }

        /// <summary>
        /// Obtient le ViewModel associé à cette fenêtre
        /// </summary>
        public PreviewWindowViewModel ViewModel => _viewModel;

        /// <summary>
        /// Obtient ou définit le facteur d'échelle visuelle pour la prévisualisation
        /// </summary>
        public double ScaleFactor
        {
            get { return (_viewModel != null) ? _viewModel.ScaleFactor : 1.0; }
            set
            {
                if (_viewModel != null)
                    _viewModel.ScaleFactor = value;
            }
        }

        /// <summary>
        /// Obtient ou définit l'opacité du fond de la fenêtre
        /// </summary>
        public double BackgroundOpacity
        {
            get { return (_viewModel != null) ? _viewModel.BackgroundOpacity : 0.2; }
            set
            {
                if (_viewModel != null)
                    _viewModel.BackgroundOpacity = value;
            }
        }

        #endregion

        #region Constructeur et initialisation

        /// <summary>
        /// Initialise une nouvelle instance de la classe PreviewWindow
        /// </summary>
        public PreviewWindow()
        {
            // Créer et initialiser le ViewModel
            _viewModel = new PreviewWindowViewModel();
            DataContext = _viewModel;

            // Initialiser la fenêtre (apparence et comportement)
            InitializeWindow();

            // Créer l'interface utilisateur
            CreateUI();

            // S'abonner aux événements
            SubscribeEvents();
        }

        /// <summary>
        /// Initialise les propriétés de la fenêtre
        /// </summary>
        private void InitializeWindow()
        {
            // Définir les propriétés de la fenêtre
            Title = "Prévisualisation";
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            Background = Brushes.Transparent;
            AllowsTransparency = true;
            SizeToContent = SizeToContent.Manual;
            WindowStartupLocation = WindowStartupLocation.Manual;

            // Définir une taille par défaut
            Width = 400;
            Height = 300;

            // Créer une bordure semi-transparente
            BorderThickness = new Thickness(1);
            BorderBrush = new SolidColorBrush(Color.FromArgb(128, 0, 120, 215));

            // Ajouter un effet d'ombre pour améliorer la visibilité
            Effect = new DropShadowEffect
            {
                ShadowDepth = 5,
                BlurRadius = 10,
                Color = Colors.Black,
                Opacity = 0.5,
                RenderingBias = RenderingBias.Performance
            };

            // Détecter le facteur d'échelle DPI actuel
            DetectDpiScaling();
        }

        /// <summary>
        /// Détecte le facteur d'échelle DPI pour le moniteur actuel
        /// </summary>
        private void DetectDpiScaling()
        {
            try
            {
                // Obtenir le DPI du système
                // Utilisez la méthode statique d'extension pour obtenir le facteur d'échelle
                // pour éviter la duplication de code
                HelloWorld.ScreenUtility.MonitorInfo monitor = null;

                // Si la fenêtre target est définie, utiliser son moniteur
                if (_targetWindow != null)
                {
                    monitor = WindowPositioningHelper.FindMonitorContainingWindow(_targetWindow);
                }

                // Sinon, utiliser le moniteur principal
                if (monitor == null)
                {
                    monitor = HelloWorld.ScreenUtility.PrimaryMonitor;
                }

                // Obtenir le facteur d'échelle
                if (monitor != null)
                {
                    _currentDpiScaleFactor = WindowPositioningHelper.GetDpiScaleFactor(monitor);
                }
                else
                {
                    // Fallback: utiliser PresentationSource si disponible
                    // Cette méthode ne fonctionne qu'une fois la fenêtre chargée
                    PresentationSource source = PresentationSource.FromVisual(this);
                    if (source != null && source.CompositionTarget != null)
                    {
                        Matrix m = source.CompositionTarget.TransformToDevice;
                        _currentDpiScaleFactor = m.M11; // Le facteur d'échelle horizontal
                    }
                }

                // Journaliser le facteur d'échelle détecté
                System.Diagnostics.Debug.WriteLine($"PreviewWindow: Facteur d'échelle DPI détecté: {_currentDpiScaleFactor:F2}");
            }
            catch (Exception ex)
            {
                // En cas d'erreur, utiliser la valeur par défaut
                _currentDpiScaleFactor = 1.0;
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la détection du DPI: {ex.Message}");
            }
        }
        /// <summary>
        /// Crée l'interface utilisateur de la fenêtre
        /// </summary>
        private void CreateUI()
        {
            // Créer une grille principale
            Grid mainGrid = new Grid();

            // Créer un Border pour l'apparence et l'effet visuel
            Border mainBorder = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0)
            };

            // Lier les propriétés visuelles au ViewModel
            mainBorder.SetBinding(Border.BorderBrushProperty, "BorderBrush");
            mainBorder.SetBinding(Border.BackgroundProperty, "Background");

            // Ajouter un conteneur pour le contenu visuel
            _contentHost = new ContentControl
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0)
            };

            // Note: Nous n'ajoutons pas de TextBlock pour les dimensions ici
            // car elles seront affichées par le renderer lui-même

            // Assembler les éléments
            mainBorder.Child = _contentHost;
            mainGrid.Children.Add(mainBorder);

            // Définir le contenu de la fenêtre
            Content = mainGrid;
        }

        /// <summary>
        /// S'abonne aux événements de la fenêtre et du ViewModel
        /// </summary>
        private void SubscribeEvents()
        {
            // S'abonner aux événements de la souris pour permettre le déplacement de la fenêtre
            MouseLeftButtonDown += PreviewWindow_MouseLeftButtonDown;
            MouseLeftButtonUp += PreviewWindow_MouseLeftButtonUp;
            MouseMove += PreviewWindow_MouseMove;
            KeyDown += PreviewWindow_KeyDown;

            // S'abonner à l'événement Loaded
            Loaded += PreviewWindow_Loaded;

            // S'abonner aux événements du ViewModel
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        #endregion

        #region Configuration du renderer et de la fenêtre cible

        /// <summary>
        /// Définit le renderer à utiliser pour la prévisualisation
        /// </summary>
        /// <param name="renderer">Renderer à utiliser</param>
        /// <exception cref="ArgumentNullException">Lancée si renderer est null</exception>
        public void SetRenderer(IPreviewRenderer renderer)
        {
            // Vérifier que le paramètre est valide
            if (renderer == null)
            {
                throw new ArgumentNullException(nameof(renderer), "Le renderer ne peut pas être null");
            }

            // Stocker la référence au renderer
            _renderer = renderer;

            // Mettre à jour le contenu visuel
            UpdateVisual();

            // Traçage pour le débogage
            System.Diagnostics.Debug.WriteLine("PreviewWindow: Renderer défini et contenu visuel mis à jour");
        }

        /// <summary>
        /// Définit la fenêtre cible dont on prévisualise les dimensions
        /// </summary>
        /// <param name="targetWindow">Fenêtre cible</param>
        /// <exception cref="ArgumentNullException">Lancée si targetWindow est null</exception>
        public void SetTargetWindow(Window targetWindow)
        {
            // Vérifier que le paramètre est valide
            if (targetWindow == null)
            {
                throw new ArgumentNullException(nameof(targetWindow), "La fenêtre cible ne peut pas être null");
            }

            // Stocker la référence à la fenêtre cible
            _targetWindow = targetWindow;

            // Mettre à jour le titre et le ViewModel
            if (_viewModel != null)
            {
                _viewModel.WindowTitle = $"Prévisualisation de {_targetWindow.Title}";
            }

            Title = $"Prévisualisation de {_targetWindow.Title}";

            // Synchroniser l'icône avec la fenêtre cible
            try
            {
                Icon = _targetWindow.Icon;
            }
            catch (Exception ex)
            {
                // Ignorer les erreurs liées à l'icône
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la synchronisation de l'icône: {ex.Message}");
            }
        }

        #endregion

        #region Gestion de l'affichage et mises à jour visuelles

        /// <summary>
        /// Met à jour le contenu visuel de la prévisualisation
        /// en tenant compte du facteur d'échelle DPI
        /// </summary>
        private void UpdateVisual()
        {
            // Vérifier que le renderer est défini
            if (_renderer == null)
            {
                System.Diagnostics.Debug.WriteLine("UpdateVisual: Le renderer n'est pas défini");
                return;
            }

            try
            {
                // Mettre à jour le facteur d'échelle DPI
                DetectDpiScaling();

                // Créer le contenu visuel avec le renderer courant
                FrameworkElement visualElement = _renderer.CreateVisualElement();

                // Calculer le facteur d'échelle total en tenant compte à la fois
                // du facteur d'échelle DPI et du facteur d'échelle défini par l'utilisateur
                double totalScaleFactor = ScaleFactor;

                // N'appliquer le scaling sur l'élément visuel que si nécessaire
                if (Math.Abs(totalScaleFactor - 1.0) > 0.01) // Si le facteur d'échelle n'est pas ~1.0
                {
                    // Appliquer la transformation d'échelle
                    visualElement.LayoutTransform = new ScaleTransform(totalScaleFactor, totalScaleFactor);
                }

                // Mettre à jour le contenu du conteneur
                _contentHost.Content = visualElement;

                // Mettre à jour les dimensions affichées dans le ViewModel
                if (_viewModel != null)
                {
                    // Utiliser les dimensions réelles pour l'affichage
                    int width = (int)this.Width;
                    int height = (int)this.Height;

                    // Mettre à jour les dimensions dans le ViewModel
                    _viewModel.UpdateDimensions(width, height);

                    // Activer l'affichage des dimensions
                    _viewModel.ShowDimensions = true;

                    // Journaliser pour le débogage
                    System.Diagnostics.Debug.WriteLine($"PreviewWindow.UpdateVisual: Mise à jour des dimensions à {width}x{height}");
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la mise à jour du contenu visuel: {ex.Message}");

                // Afficher un message d'erreur
                TextBlock errorText = new TextBlock
                {
                    Text = "Erreur de prévisualisation",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.Red,
                    FontWeight = FontWeights.Bold
                };

                _contentHost.Content = errorText;
            }
        }

        /// <summary>
        /// Adapte l'apparence de la fenêtre en fonction de son état
        /// </summary>
        private void UpdateWindowAppearance()
        {
            if (_viewModel == null) return;

            // Adapter l'apparence en fonction de l'état de déplacement
            if (_isDragging)
            {
                // Apparence pendant le déplacement (plus visible)
                _viewModel.BorderBrush = new SolidColorBrush(Color.FromArgb(200, 0, 120, 215));
                _viewModel.BackgroundOpacity = 0.3; // Plus opaque pendant le déplacement
            }
            else
            {
                // Apparence normale
                _viewModel.BorderBrush = new SolidColorBrush(Color.FromArgb(128, 0, 120, 215));
                _viewModel.BackgroundOpacity = 0.1; // Plus transparent au repos
            }
        }

        /// <summary>
        /// Met à jour les dimensions visuelles de la prévisualisation
        /// en tenant compte du facteur d'échelle DPI
        /// </summary>
        /// <param name="newWidth">Nouvelle largeur</param>
        /// <param name="newHeight">Nouvelle hauteur</param>
        public void UpdateDimensions(double newWidth, double newHeight)
        {
            try
            {
                // Vérifier que les dimensions sont valides
                if (newWidth <= 0 || newHeight <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateDimensions: Dimensions invalides {newWidth}x{newHeight}");
                    return;
                }

                // Journaliser pour le débogage
                System.Diagnostics.Debug.WriteLine($"PreviewWindow.UpdateDimensions: Mise à jour des dimensions à {newWidth}x{newHeight}");

                // Mettre à jour les dimensions de la fenêtre
                // Sans ajuster par le DPI, car le gestionnaire de preview s'en charge déjà
                Width = newWidth;
                Height = newHeight;

                // S'assurer que le ViewModel est à jour
                if (_viewModel != null)
                {
                    // Arrondir les dimensions pour un affichage plus propre
                    int roundedWidth = (int)Math.Round(newWidth);
                    int roundedHeight = (int)Math.Round(newHeight);

                    // Mettre à jour les dimensions dans le ViewModel
                    _viewModel.UpdateDimensions(roundedWidth, roundedHeight);

                    // Activer l'affichage des dimensions
                    _viewModel.ShowDimensions = true;
                }

                // Mettre à jour le rendu visuel pour assurer l'affichage des dimensions
                if (_renderer != null)
                {
                    _renderer.UpdateVisual(new Size(newWidth, newHeight));
                }

                // Force le rafraîchissement visuel
                InvalidateVisual();
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur pour le débogage
                System.Diagnostics.Debug.WriteLine($"Erreur dans UpdateDimensions: {ex.Message}");
            }
        }

        #endregion

        #region Nouveaux gestionnaires d'événements pour le DPI
        
        /// <summary>
        /// Gestionnaire pour l'événement de changement de DPI (Windows 10+)
        /// </summary>
        private void OnDpiChanged(object sender, EventArgs e)
        {
            // Mettre à jour le facteur d'échelle DPI
            DetectDpiScaling();

            // Mettre à jour le contenu visuel
            UpdateVisual();
        }

        /// <summary>
        /// Méthode pour s'abonner aux événements relatifs au DPI
        /// </summary>
        private void SubscribeToDpiEvents()
        {
            try
            {
                // S'abonner à l'événement DpiChanged de la fenêtre si disponible
                // Note: Ceci nécessite Windows 10 ou supérieur
                HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
                if (source != null)
                {
                    // Tentative d'abonnement à l'événement DpiChanged via réflexion
                    // car il peut ne pas être disponible sur toutes les versions de Windows
                    var dpiChangedEvent = typeof(HwndSource).GetEvent("DpiChanged");
                    if (dpiChangedEvent != null)
                    {
                        // Créer un délégué pour l'événement
                        var handler = Delegate.CreateDelegate(dpiChangedEvent.EventHandlerType, this,
                            typeof(PreviewWindow).GetMethod("OnDpiChanged",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));

                        // S'abonner à l'événement
                        dpiChangedEvent.AddEventHandler(source, handler);
                    }
                }
            }
            catch (Exception ex)
            {
                // Ignorer les erreurs, car cette fonctionnalité est optionnelle
                System.Diagnostics.Debug.WriteLine($"Impossible de s'abonner aux événements DPI: {ex.Message}");
            }
        }

        /// <summary>
        /// Méthode pour se désabonner des événements relatifs au DPI
        /// </summary>
        private void UnsubscribeFromDpiEvents()
        {
            try
            {
                // Se désabonner de l'événement DpiChanged
                HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
                if (source != null)
                {
                    var dpiChangedEvent = typeof(HwndSource).GetEvent("DpiChanged");
                    if (dpiChangedEvent != null)
                    {
                        // Créer un délégué pour l'événement
                        var handler = Delegate.CreateDelegate(dpiChangedEvent.EventHandlerType, this,
                            typeof(PreviewWindow).GetMethod("OnDpiChanged",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));

                        // Se désabonner de l'événement
                        dpiChangedEvent.RemoveEventHandler(source, handler);
                    }
                }
            }
            catch
            {
                // Ignorer les erreurs
            }
        }

        #endregion

        #region Gestionnaires d'événements

        /// <summary>
        /// Gestionnaire de l'événement Loaded de la fenêtre
        /// </summary>
        private void PreviewWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Détecter le facteur d'échelle DPI une fois la fenêtre chargée
            // pour une détection plus précise
            DetectDpiScaling();

            // Mettre à jour le contenu visuel
            UpdateVisual();

            // Ajuster l'apparence de la fenêtre
            UpdateWindowAppearance();

            // Configurer la fenêtre pour la prévisualisation
            ConfigurePreviewWindowStyle();
        }

        /// <summary>
        /// Gestionnaire de l'événement MouseLeftButtonDown de la fenêtre
        /// </summary>
        private void PreviewWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Si la fenêtre n'est pas déplaçable, ne rien faire
            if (!IsDraggable)
            {
                return;
            }

            // Capturer la souris pour recevoir les événements même si le curseur quitte la fenêtre
            CaptureMouse();

            // Enregistrer la position de départ
            _dragStartPosition = e.GetPosition(this);

            // Marquer que la fenêtre est en train d'être déplacée
            _isDragging = true;

            // Mettre à jour l'apparence
            UpdateWindowAppearance();

            // Indiquer que l'événement a été traité
            e.Handled = true;
        }

        /// <summary>
        /// Gestionnaire de l'événement MouseLeftButtonUp de la fenêtre
        /// </summary>
        private void PreviewWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Si la fenêtre n'est pas en train d'être déplacée, ne rien faire
            if (!_isDragging)
            {
                return;
            }

            // Libérer la capture de la souris
            ReleaseMouseCapture();

            // Marquer que la fenêtre n'est plus en train d'être déplacée
            _isDragging = false;

            // Mettre à jour l'apparence
            UpdateWindowAppearance();

            // Indiquer que l'événement a été traité
            e.Handled = true;
        }

        /// <summary>
        /// Gestionnaire de l'événement MouseMove de la fenêtre
        /// </summary>
        private void PreviewWindow_MouseMove(object sender, MouseEventArgs e)
        {
            // Si la fenêtre n'est pas en train d'être déplacée, ne rien faire
            if (!_isDragging)
            {
                return;
            }

            // Calculer le déplacement relatif à la position de départ
            Point currentPosition = e.GetPosition(this);
            Vector offset = currentPosition - _dragStartPosition;

            // Déplacer la fenêtre
            Left += offset.X;
            Top += offset.Y;

            // Indiquer que l'événement a été traité
            e.Handled = true;
        }

        /// <summary>
        /// Gestionnaire de l'événement KeyDown de la fenêtre
        /// </summary>
        private void PreviewWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Fermer la fenêtre si la touche Echap est pressée
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement PropertyChanged du ViewModel
        /// </summary>
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Réagir aux changements de propriétés du ViewModel
            switch (e.PropertyName)
            {
                case nameof(PreviewWindowViewModel.ScaleFactor):
                    // Mettre à jour la mise à l'échelle du contenu
                    UpdateVisual();
                    break;

                case nameof(PreviewWindowViewModel.ShowDimensions):
                    // La visibilité est automatiquement mise à jour via le binding
                    break;

                case nameof(PreviewWindowViewModel.BackgroundOpacity):
                    // L'opacité est automatiquement mise à jour via le binding dans le XAML
                    break;
            }
        }

        #endregion

        #region Méthodes d'interopérabilité avec le système d'exploitation

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        /// <summary>
        /// Configure le style de la fenêtre pour qu'elle se comporte comme une prévisualisation
        /// et enregistre les gestionnaires d'événements pour le DPI
        /// </summary>
        private void ConfigurePreviewWindowStyle()
        {
            try
            {
                // Obtenir le handle de la fenêtre
                IntPtr hwnd = new WindowInteropHelper(this).Handle;

                // Modifier le style étendu pour désactiver l'apparition dans la barre des tâches
                // et éviter l'activation (ne vole pas le focus)
                int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

                // S'abonner aux événements de changement de DPI
                SubscribeToDpiEvents();
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la configuration du style de la fenêtre: {ex.Message}");
            }
        }

        #endregion

        #region Nettoyage des ressources

        /// <summary>
        /// Libère les ressources et se désabonne des événements
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // Se désabonner des événements DPI
            UnsubscribeFromDpiEvents();

            base.OnClosed(e);

            // Se désabonner des événements pour éviter les fuites mémoire
            MouseLeftButtonDown -= PreviewWindow_MouseLeftButtonDown;
            MouseLeftButtonUp -= PreviewWindow_MouseLeftButtonUp;
            MouseMove -= PreviewWindow_MouseMove;
            KeyDown -= PreviewWindow_KeyDown;
            Loaded -= PreviewWindow_Loaded;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                _viewModel = null;
            }

            // Nettoyer les références
            _targetWindow = null;

            if (_renderer != null)
            {
                _renderer.Cleanup();
                _renderer = null;
            }

            _contentHost = null;
        }

        #endregion
    }
}