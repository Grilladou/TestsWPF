using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Media.Effects;

namespace HelloWorld.Preview
{
    /// <summary>
    /// Renderer qui dessine un simple contour avec des informations de dimension.
    /// C'est le renderer le plus léger et le plus rapide.
    /// </summary>
    public class OutlinePreviewRenderer : IPreviewRenderer
    {
        #region Champs privés

        // Référence à la fenêtre cible
        private Window _targetWindow;

        // Éléments visuels
        private Grid _mainGrid;
        private Border _outlineBorder;

        // Texte pour l'affichage des dimensions
        private TextBlock _dimensionsText;

        // Type d'indicateur de dimensions utilisé pour l'affichage
        private DimensionIndicatorType _dimensionIndicatorType = DimensionIndicatorType.PixelsOnly;

        #endregion

        #region Propriétés publiques

        /// <summary>
        /// Obtient ou définit le type d'indicateur de dimensions utilisé pour l'affichage
        /// </summary>
        public DimensionIndicatorType DimensionIndicatorType
        {
            get { return _dimensionIndicatorType; }
            set { _dimensionIndicatorType = value; }
        }

        #endregion

        #region Implémentation de IPreviewRenderer

        /// <summary>
        /// Initialise le renderer avec la fenêtre cible
        /// </summary>
        /// <param name="targetWindow">Fenêtre cible à prévisualiser</param>
        /// <exception cref="ArgumentNullException">Lancée si targetWindow est null</exception>
        public void Initialize(Window targetWindow)
        {
            // Vérifier que le paramètre est valide
            if (targetWindow == null)
            {
                throw new ArgumentNullException(nameof(targetWindow), "La fenêtre cible ne peut pas être null");
            }

            // Stocker la référence à la fenêtre cible
            _targetWindow = targetWindow;
        }

        /// <summary>
        /// Crée l'élément visuel qui sera affiché dans la fenêtre de prévisualisation
        /// </summary>
        /// <returns>L'élément visuel à afficher</returns>
        public FrameworkElement CreateVisualElement()
        {
            try
            {
                // Créer la grille principale
                _mainGrid = new Grid();

                // Créer la bordure de contour
                _outlineBorder = new Border
                {
                    BorderThickness = new Thickness(2),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(180, 0, 120, 215)),
                    Background = new SolidColorBrush(Color.FromArgb(20, 0, 120, 215)),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Margin = new Thickness(0)
                };

                // Créer le texte des dimensions au centre
                _dimensionsText = new TextBlock
                {
                    FontSize = 22,                      // Taille de police
                    FontWeight = FontWeights.Bold,      // Texte en gras
                    Foreground = new SolidColorBrush(Color.FromArgb(220, 0, 120, 215)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0),          // Pas de marge
                    // Ajouter TextWrapping pour permettre les sauts de ligne
                    TextWrapping = TextWrapping.Wrap,
                    // Centre le texte horizontalement pour les deux lignes
                    TextAlignment = TextAlignment.Center
                };

                // Ajouter un effet d'ombre pour améliorer la lisibilité
                _dimensionsText.Effect = new DropShadowEffect
                {
                    ShadowDepth = 0,
                    BlurRadius = 4,
                    Color = Colors.White,
                    Opacity = 0.8
                };

                // NE PAS initialiser avec "..." mais avec des dimensions valides
                if (_targetWindow != null)
                {
                    // Utiliser une valeur par défaut sensible si on n'a pas encore les dimensions du preview
                    _dimensionsText.Text = "Dimensions";
                }

                // Ajouter les éléments à la grille
                _mainGrid.Children.Add(_outlineBorder);
                _mainGrid.Children.Add(_dimensionsText);

                return _mainGrid;
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur dans CreateVisualElement: {ex.Message}");

                // Créer un élément minimal en cas d'erreur
                TextBlock errorText = new TextBlock
                {
                    Text = "Erreur d'affichage",
                    Foreground = new SolidColorBrush(Colors.Red),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                return errorText;
            }
        }

        /// <summary>
        /// Met à jour le rendu visuel avec les nouvelles dimensions
        /// </summary>
        /// <param name="newSize">Nouvelles dimensions à prévisualiser</param>
        public void UpdateVisual(Size newSize)
        {
            try
            {
                // Vérifier que le texte des dimensions existe
                if (_dimensionsText == null)
                {
                    System.Diagnostics.Debug.WriteLine("UpdateVisual: _dimensionsText est null - initialisation incorrecte");
                    return;
                }

                // Mettre à jour le texte des dimensions
                string dimensionsText = GetDimensionsText(newSize.Width, newSize.Height);
                _dimensionsText.Text = dimensionsText;

                // Forcer la mise à jour visuelle
                _dimensionsText.InvalidateVisual();

                // Journaliser la mise à jour pour le débogage
                System.Diagnostics.Debug.WriteLine($"OutlinePreviewRenderer.UpdateVisual: Dimensions mises à jour à {dimensionsText}");
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur pour le débogage
                System.Diagnostics.Debug.WriteLine($"Erreur dans UpdateVisual: {ex.Message}");
            }
        }

        /// <summary>
        /// Nettoie les ressources utilisées par le renderer
        /// </summary>
        public void Cleanup()
        {
            // Libérer les références
            _targetWindow = null;
            _mainGrid = null;
            _outlineBorder = null;
            _dimensionsText = null;
        }

        #endregion

        #region Méthodes utilitaires privées

        /// <summary>
        /// Génère le texte de dimensions à afficher avec un format lisible
        /// selon le type d'indicateur sélectionné
        /// </summary>
        /// <param name="width">Largeur en pixels</param>
        /// <param name="height">Hauteur en pixels</param>
        /// <returns>Le texte formaté des dimensions</returns>
        private string GetDimensionsText(double width, double height)
        {
            // Utiliser la classe utilitaire pour formater les dimensions
            return DimensionsFormatter.FormatDimensions(width, height, _dimensionIndicatorType);
        }

        #endregion
    }

    /// <summary>
    /// Renderer qui crée une miniature visuelle de la fenêtre cible.
    /// Ce renderer est plus élaboré et offre une prévisualisation plus détaillée.
    /// </summary>
    public class ThumbnailPreviewRenderer : IPreviewRenderer
    {
        #region Champs privés

        // Référence à la fenêtre cible
        private Window _targetWindow;

        // Éléments visuels
        private Grid _mainGrid;
        private Border _windowBorder;
        private Rectangle _titleBar;
        private TextBlock _titleText;
        private StackPanel _windowButtons;
        private Image _thumbnailImage;
        private TextBlock _dimensionsText;
        private Image _logoImage;

        // Couleurs pour le rendu
        private Color _windowBackground = Color.FromRgb(240, 240, 240);
        private Color _titleBarColor = Color.FromRgb(220, 220, 220);
        private Color _titleTextColor = Color.FromRgb(30, 30, 30);
        private Color _borderColor = Color.FromRgb(180, 180, 180);

        // Capture d'écran
        private BitmapSource _capturedImage;

        // Type d'indicateur de dimensions utilisé pour l'affichage
        private DimensionIndicatorType _dimensionIndicatorType = DimensionIndicatorType.PixelsOnly;

        #endregion

        #region Propriétés publiques

        /// <summary>
        /// Obtient ou définit le type d'indicateur de dimensions utilisé pour l'affichage
        /// </summary>
        public DimensionIndicatorType DimensionIndicatorType
        {
            get { return _dimensionIndicatorType; }
            set { _dimensionIndicatorType = value; }
        }

        #endregion

        #region Implémentation de IPreviewRenderer

        /// <summary>
        /// Initialise le renderer avec la fenêtre cible
        /// </summary>
        /// <param name="targetWindow">Fenêtre cible à prévisualiser</param>
        /// <exception cref="ArgumentNullException">Lancée si targetWindow est null</exception>
        public void Initialize(Window targetWindow)
        {
            // Vérifier que le paramètre est valide
            if (targetWindow == null)
            {
                throw new ArgumentNullException(nameof(targetWindow), "La fenêtre cible ne peut pas être null");
            }

            // Stocker la référence à la fenêtre cible
            _targetWindow = targetWindow;

            // Capturer une image de la fenêtre
            TryCaptureWindowImage();
        }

        /// <summary>
        /// Crée l'élément visuel qui sera affiché dans la fenêtre de prévisualisation.
        /// Version améliorée avec vérification de référence null sur targetWindow.
        /// </summary>
        /// <returns>L'élément visuel à afficher</returns>
        public FrameworkElement CreateVisualElement()
        {
            try
            {
                // Créer la grille principale
                _mainGrid = new Grid();

                // Créer la bordure de la fenêtre
                _windowBorder = new Border
                {
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(_borderColor),
                    Background = new SolidColorBrush(_windowBackground),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Margin = new Thickness(0),
                    CornerRadius = new CornerRadius(3),
                    Effect = new DropShadowEffect
                    {
                        ShadowDepth = 2,
                        BlurRadius = 4,
                        Color = Colors.Black,
                        Opacity = 0.3
                    }
                };

                // Créer le contenu de la fenêtre
                Grid windowContent = new Grid();
                windowContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) }); // Barre de titre
                windowContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Dimensions
                windowContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Contenu

                // Créer la barre de titre
                _titleBar = new Rectangle
                {
                    Fill = new SolidColorBrush(_titleBarColor),
                    RadiusX = 3,
                    RadiusY = 3
                };
                windowContent.Children.Add(_titleBar);
                Grid.SetRow(_titleBar, 0);

                // MODIFICATION IMPORTANTE: Vérifier que _targetWindow n'est pas null avant d'accéder à ses propriétés
                // Créer le texte du titre avec une vérification de null
                _titleText = new TextBlock
                {
                    // Utiliser une valeur par défaut si _targetWindow est null
                    Text = (_targetWindow != null) ? _targetWindow.Title : "Prévisualisation",
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0),
                    Foreground = new SolidColorBrush(_titleTextColor),
                    FontSize = 12
                };
                windowContent.Children.Add(_titleText);
                Grid.SetRow(_titleText, 0);

                // Créer les boutons de la fenêtre
                _windowButtons = CreateWindowButtons();
                windowContent.Children.Add(_windowButtons);
                Grid.SetRow(_windowButtons, 0);

                // Créer le texte des dimensions - avec vérification de null sur _targetWindow
                string initialDimensions = "Dimensions";
                if (_targetWindow != null)
                {
                    initialDimensions = GetDimensionsText(_targetWindow.Width, _targetWindow.Height);
                }

                _dimensionsText = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 5),
                    Foreground = new SolidColorBrush(Colors.DimGray),
                    // Ajouter TextWrapping pour permettre les sauts de ligne
                    TextWrapping = TextWrapping.Wrap,
                    // Centre le texte horizontalement pour les deux lignes
                    TextAlignment = TextAlignment.Center,
                    // Initialiser avec les dimensions actuelles ou une valeur par défaut
                    Text = initialDimensions
                };
                windowContent.Children.Add(_dimensionsText);
                Grid.SetRow(_dimensionsText, 1);

                // Créer l'image miniature
                if (_capturedImage != null)
                {
                    _thumbnailImage = new Image
                    {
                        Source = _capturedImage,
                        Stretch = Stretch.Uniform,
                        Margin = new Thickness(5)
                    };
                    windowContent.Children.Add(_thumbnailImage);
                    Grid.SetRow(_thumbnailImage, 2);
                }
                else
                {
                    // Si la capture d'écran a échoué, afficher un logo ou un message
                    _logoImage = CreatePlaceholderLogo();
                    windowContent.Children.Add(_logoImage);
                    Grid.SetRow(_logoImage, 2);
                }

                // Ajouter le contenu à la bordure
                _windowBorder.Child = windowContent;

                // Ajouter la bordure à la grille principale
                _mainGrid.Children.Add(_windowBorder);

                return _mainGrid;
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur dans CreateVisualElement de ThumbnailPreviewRenderer: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // Créer un élément minimal en cas d'erreur
                TextBlock errorText = new TextBlock
                {
                    Text = "Erreur d'affichage",
                    Foreground = new SolidColorBrush(Colors.Red),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                return errorText;
            }
        }

        /// <summary>
        /// Met à jour le rendu visuel avec les nouvelles dimensions
        /// </summary>
        /// <param name="newSize">Nouvelles dimensions à prévisualiser</param>
        public void UpdateVisual(Size newSize)
        {
            // Mettre à jour le texte des dimensions
            if (_dimensionsText != null)
            {
                _dimensionsText.Text = GetDimensionsText(newSize.Width, newSize.Height);
            }

            // Ajouter d'autres mises à jour visuelles ici si nécessaire
        }

        /// <summary>
        /// Nettoie les ressources utilisées par le renderer
        /// </summary>
        public void Cleanup()
        {
            // Libérer la capture d'écran
            _capturedImage = null;

            // Libérer les références
            _targetWindow = null;
            _mainGrid = null;
            _windowBorder = null;
            _titleBar = null;
            _titleText = null;
            _windowButtons = null;
            _thumbnailImage = null;
            _dimensionsText = null;
            _logoImage = null;
        }

        #endregion

        #region Méthodes utilitaires privées

        /// <summary>
        /// Génère le texte de dimensions à afficher avec un format lisible
        /// selon le type d'indicateur sélectionné
        /// </summary>
        /// <param name="width">Largeur en pixels</param>
        /// <param name="height">Hauteur en pixels</param>
        /// <returns>Le texte formaté des dimensions</returns>
        private string GetDimensionsText(double width, double height)
        {
            // Utiliser la classe utilitaire pour formater les dimensions
            // Avec un label "Dimensions:" pour ThumbnailPreviewRenderer
            return DimensionsFormatter.FormatDimensions(width, height, _dimensionIndicatorType, true);
        }

        /// <summary>
        /// Crée une représentation visuelle des boutons de la fenêtre (minimiser, maximiser, fermer)
        /// </summary>
        /// <returns>Un StackPanel contenant les boutons</returns>
        private StackPanel CreateWindowButtons()
        {
            // Créer le conteneur
            StackPanel buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 5, 5, 0)
            };

            // Créer les boutons
            Ellipse minimizeButton = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new SolidColorBrush(Colors.Gold),
                Margin = new Thickness(2)
            };

            Ellipse maximizeButton = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new SolidColorBrush(Colors.LimeGreen),
                Margin = new Thickness(2)
            };

            Ellipse closeButton = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new SolidColorBrush(Colors.Tomato),
                Margin = new Thickness(2)
            };

            // Ajouter les boutons au conteneur
            buttons.Children.Add(minimizeButton);
            buttons.Children.Add(maximizeButton);
            buttons.Children.Add(closeButton);

            return buttons;
        }

        /// <summary>
        /// Crée un logo ou une image de remplacement quand la capture d'écran n'est pas disponible
        /// </summary>
        /// <returns>Un contrôle Image contenant le logo</returns>
        private Image CreatePlaceholderLogo()
        {
            // Créer un DrawingVisual pour dessiner le logo
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                // Dessiner un fond
                Rect backgroundRect = new Rect(0, 0, 200, 150);
                drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)), null, backgroundRect);

                // Dessiner un logo simple (par exemple une fenêtre stylisée)
                Rect windowRect = new Rect(50, 30, 100, 80);
                drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(50, 0, 120, 215)),
                    new Pen(new SolidColorBrush(Color.FromArgb(100, 0, 120, 215)), 2), windowRect);

                // Dessiner une barre de titre stylisée
                Rect titleRect = new Rect(50, 30, 100, 20);
                drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(80, 0, 120, 215)), null, titleRect);

                // Dessiner un texte
                FormattedText text = new FormattedText(
                    "Aperçu",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    14,
                    new SolidColorBrush(Colors.DimGray),
                    VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                drawingContext.DrawText(text, new Point(
                    windowRect.Left + (windowRect.Width - text.Width) / 2,
                    windowRect.Top + (windowRect.Height - text.Height) / 2 + 10));
            }

            // Convertir le DrawingVisual en RenderTargetBitmap
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                200,
                150,
                96,
                96,
                PixelFormats.Pbgra32);

            rtb.Render(drawingVisual);

            // Créer et retourner l'image
            return new Image
            {
                Source = rtb,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(5)
            };
        }

        /// <summary>
        /// Tente de capturer une image de la fenêtre cible avec une vérification améliorée
        /// pour s'assurer que la fenêtre cible est valide.
        /// </summary>
        private void TryCaptureWindowImage()
        {
            try
            {
                // Vérification de null sur _targetWindow
                if (_targetWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("TryCaptureWindowImage: La fenêtre cible est null, impossible de capturer l'image");
                    return;
                }

                // Obtenir le handle de la fenêtre avec vérification de validité
                var helper = new WindowInteropHelper(_targetWindow);
                IntPtr hwnd = helper.Handle;

                // Vérifier que le handle est valide
                if (hwnd == IntPtr.Zero)
                {
                    System.Diagnostics.Debug.WriteLine("TryCaptureWindowImage: Handle de fenêtre invalide (IntPtr.Zero)");
                    return;
                }

                // Capturer une image de la fenêtre
                _capturedImage = CaptureWindow(hwnd);

                // Journaliser le résultat
                if (_capturedImage != null)
                {
                    System.Diagnostics.Debug.WriteLine("TryCaptureWindowImage: Image capturée avec succès");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("TryCaptureWindowImage: Échec de la capture d'image, _capturedImage est null");
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la capture d'écran: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // Laisser _capturedImage null pour utiliser l'image de remplacement
            }
        }

        #endregion

        #region Méthodes d'interopérabilité avec le système d'exploitation

        // Constantes et structures pour l'API Win32

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        /// <summary>
        /// Capture une image de la fenêtre spécifiée
        /// </summary>
        /// <param name="hwnd">Handle de la fenêtre à capturer</param>
        /// <returns>Une BitmapSource contenant l'image capturée</returns>
        private BitmapSource CaptureWindow(IntPtr hwnd)
        {
            // Obtenir les dimensions de la fenêtre
            RECT rect;
            if (!GetWindowRect(hwnd, out rect))
            {
                return null;
            }

            int width = rect.Width;
            int height = rect.Height;

            // Créer une bitmap pour la capture
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap);

            // Obtenir le handle du contexte de périphérique de la bitmap
            IntPtr hdcBitmap = graphics.GetHdc();

            try
            {
                // Capturer la fenêtre
                if (!PrintWindow(hwnd, hdcBitmap, 0))
                {
                    return null;
                }
            }
            finally
            {
                // Libérer le handle du contexte de périphérique
                graphics.ReleaseHdc(hdcBitmap);
                graphics.Dispose();
            }

            // Convertir la bitmap en BitmapSource
            BitmapSource bitmapSource = null;
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Rendre l'image immuable pour une utilisation entre threads

                bitmapSource = bitmapImage;
            }

            // Libérer la bitmap
            bitmap.Dispose();

            return bitmapSource;
        }

        #endregion
    }

    /// <summary>
    /// Renderer qui simule l'apparence de la fenêtre avec un contenu simplifié.
    /// Ce renderer offre un compromis entre performance et fidélité visuelle.
    /// </summary>
    public class SimulatedPreviewRenderer : IPreviewRenderer
    {
        #region Champs privés

        // Référence à la fenêtre cible
        private Window _targetWindow;

        // Éléments visuels
        private Grid _mainGrid;
        private Border _windowBorder;
        private DockPanel _windowChrome;
        private TextBlock _titleText;
        private StackPanel _windowContent;
        private TextBlock _dimensionsText;

        // Couleurs pour le rendu
        private SolidColorBrush _accentColor = new SolidColorBrush(Color.FromRgb(0, 120, 215));
        private SolidColorBrush _backgroundColor = new SolidColorBrush(Colors.White);
        private SolidColorBrush _textColor = new SolidColorBrush(Colors.Black);

        // Type d'indicateur de dimensions utilisé pour l'affichage
        private DimensionIndicatorType _dimensionIndicatorType = DimensionIndicatorType.PixelsOnly;

        #endregion

        #region Propriétés publiques

        /// <summary>
        /// Obtient ou définit le type d'indicateur de dimensions utilisé pour l'affichage
        /// </summary>
        public DimensionIndicatorType DimensionIndicatorType
        {
            get { return _dimensionIndicatorType; }
            set { _dimensionIndicatorType = value; }
        }

        #endregion

        #region Implémentation de IPreviewRenderer

        /// <summary>
        /// Initialise le renderer avec la fenêtre cible
        /// </summary>
        /// <param name="targetWindow">Fenêtre cible à prévisualiser</param>
        /// <exception cref="ArgumentNullException">Lancée si targetWindow est null</exception>
        public void Initialize(Window targetWindow)
        {
            // Vérifier que le paramètre est valide
            if (targetWindow == null)
            {
                throw new ArgumentNullException(nameof(targetWindow), "La fenêtre cible ne peut pas être null");
            }

            // Stocker la référence à la fenêtre cible
            _targetWindow = targetWindow;

            // Analyser les couleurs de la fenêtre cible pour un rendu plus fidèle
            TryDetectTargetWindowColors();
        }

        /// <summary>
        /// Crée l'élément visuel qui sera affiché dans la fenêtre de prévisualisation
        /// </summary>
        /// <returns>L'élément visuel à afficher</returns>
        public FrameworkElement CreateVisualElement()
        {
            try
            {
                // Créer la grille principale
                _mainGrid = new Grid();

                // Créer la bordure de la fenêtre
                _windowBorder = new Border
                {
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Colors.LightGray),
                    Background = _backgroundColor,
                    CornerRadius = new CornerRadius(3),
                    Effect = new DropShadowEffect
                    {
                        ShadowDepth = 2,
                        BlurRadius = 4,
                        Color = Colors.Black,
                        Opacity = 0.3
                    }
                };

                // Créer un conteneur principal pour organiser le contenu
                var mainContainer = new DockPanel();

                // Créer le chrome de la fenêtre (barre de titre)
                _windowChrome = new DockPanel
                {
                    Background = _accentColor,
                    Height = 32,
                    LastChildFill = true
                };
                DockPanel.SetDock(_windowChrome, Dock.Top);

                // Ajouter le titre
                _titleText = new TextBlock
                {
                    Text = _targetWindow.Title,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0),
                    FontSize = 13
                };
                _windowChrome.Children.Add(_titleText);

                // Ajouter les boutons de la fenêtre (minimiser, maximiser, fermer)
                var windowButtons = CreateWindowButtons();
                DockPanel.SetDock(windowButtons, Dock.Right);
                _windowChrome.Children.Add(windowButtons);

                // Créer le contenu simulé de la fenêtre
                _windowContent = CreateSimulatedContent();
                DockPanel.SetDock(_windowContent, Dock.Top);

                // Ajouter le texte de dimensions
                _dimensionsText = new TextBlock
                {
                    Text = GetDimensionsText(_targetWindow.Width, _targetWindow.Height),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = new SolidColorBrush(Colors.DimGray),
                    FontStyle = FontStyles.Italic,
                    // Ajouter TextWrapping pour permettre les sauts de ligne
                    TextWrapping = TextWrapping.Wrap,
                    // Centre le texte horizontalement pour les deux lignes
                    TextAlignment = TextAlignment.Center
                };
                DockPanel.SetDock(_dimensionsText, Dock.Bottom);

                // Assembler tous les éléments
                mainContainer.Children.Add(_windowChrome);
                mainContainer.Children.Add(_dimensionsText);
                mainContainer.Children.Add(_windowContent);

                _windowBorder.Child = mainContainer;
                _mainGrid.Children.Add(_windowBorder);

                return _mainGrid;
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur dans CreateVisualElement de SimulatedPreviewRenderer: {ex.Message}");

                // Créer un élément minimal en cas d'erreur
                TextBlock errorText = new TextBlock
                {
                    Text = "Erreur d'affichage",
                    Foreground = new SolidColorBrush(Colors.Red),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                return errorText;
            }
        }

        /// <summary>
        /// Met à jour le rendu visuel avec les nouvelles dimensions
        /// </summary>
        /// <param name="newSize">Nouvelles dimensions à prévisualiser</param>
        public void UpdateVisual(Size newSize)
        {
            // Mettre à jour le texte des dimensions
            if (_dimensionsText != null)
            {
                _dimensionsText.Text = GetDimensionsText(newSize.Width, newSize.Height);
            }

            // Mettre à jour le contenu simulé en fonction des nouvelles dimensions
            UpdateSimulatedContent(newSize);
        }

        /// <summary>
        /// Nettoie les ressources utilisées par le renderer
        /// </summary>
        public void Cleanup()
        {
            // Libérer les références
            _targetWindow = null;
            _mainGrid = null;
            _windowBorder = null;
            _windowChrome = null;
            _titleText = null;
            _windowContent = null;
            _dimensionsText = null;
        }

        #endregion

        #region Méthodes utilitaires privées

        /// <summary>
        /// Génère le texte de dimensions à afficher avec un format lisible
        /// selon le type d'indicateur sélectionné
        /// </summary>
        /// <param name="width">Largeur en pixels</param>
        /// <param name="height">Hauteur en pixels</param>
        /// <returns>Le texte formaté des dimensions</returns>
        private string GetDimensionsText(double width, double height)
        {
            // Utiliser la classe utilitaire pour formater les dimensions
            return DimensionsFormatter.FormatDimensions(width, height, _dimensionIndicatorType);
        }

        /// <summary>
        /// Tente de détecter les couleurs de la fenêtre cible pour un rendu plus fidèle
        /// </summary>
        private void TryDetectTargetWindowColors()
        {
            try
            {
                // Essayer de lire les couleurs depuis la fenêtre cible
                if (_targetWindow.Background is SolidColorBrush backgroundBrush)
                {
                    _backgroundColor = new SolidColorBrush(backgroundBrush.Color);
                }

                // Essayer de détecter la couleur d'accentuation
                if (_targetWindow.Resources.Contains(SystemColors.ActiveCaptionBrushKey))
                {
                    SolidColorBrush accentBrush = _targetWindow.Resources[SystemColors.ActiveCaptionBrushKey] as SolidColorBrush;
                    if (accentBrush != null)
                    {
                        _accentColor = new SolidColorBrush(accentBrush.Color);
                    }
                }

                // Essayer de détecter la couleur du texte
                if (_targetWindow.Foreground is SolidColorBrush foregroundBrush)
                {
                    _textColor = new SolidColorBrush(foregroundBrush.Color);
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la détection des couleurs: {ex.Message}");

                // Continuer avec les couleurs par défaut
            }
        }

        /// <summary>
        /// Crée une représentation visuelle des boutons de la fenêtre (minimiser, maximiser, fermer)
        /// </summary>
        /// <returns>Un StackPanel contenant les boutons</returns>
        private StackPanel CreateWindowButtons()
        {
            // Créer le conteneur
            StackPanel buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };

            // Couleur pour l'effet de survol
            SolidColorBrush hoverColor = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));

            // Créer le bouton de minimisation
            Border minimizeButton = new Border
            {
                Width = 46,
                Height = 32,
                Background = Brushes.Transparent
            };
            Rectangle minimizeIcon = new Rectangle
            {
                Width = 10,
                Height = 1,
                Fill = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };
            minimizeButton.Child = minimizeIcon;

            // Créer le bouton de maximisation
            Border maximizeButton = new Border
            {
                Width = 46,
                Height = 32,
                Background = Brushes.Transparent
            };
            Rectangle maximizeIcon = new Rectangle
            {
                Width = 10,
                Height = 10,
                Stroke = Brushes.White,
                StrokeThickness = 1,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            maximizeButton.Child = maximizeIcon;

            // Créer le bouton de fermeture
            Border closeButton = new Border
            {
                Width = 46,
                Height = 32,
                Background = Brushes.Transparent
            };
            Grid closeGrid = new Grid();
            Line closeLine1 = new Line
            {
                X1 = 0,
                Y1 = 0,
                X2 = 10,
                Y2 = 10,
                Stroke = Brushes.White,
                StrokeThickness = 1,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Line closeLine2 = new Line
            {
                X1 = 0,
                Y1 = 10,
                X2 = 10,
                Y2 = 0,
                Stroke = Brushes.White,
                StrokeThickness = 1,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            closeGrid.Children.Add(closeLine1);
            closeGrid.Children.Add(closeLine2);
            closeButton.Child = closeGrid;

            // Ajouter les boutons au conteneur
            buttons.Children.Add(minimizeButton);
            buttons.Children.Add(maximizeButton);
            buttons.Children.Add(closeButton);

            return buttons;
        }

        /// <summary>
        /// Crée un contenu simulé pour la fenêtre en fonction de son type
        /// </summary>
        /// <returns>Un StackPanel contenant le contenu simulé</returns>
        private StackPanel CreateSimulatedContent()
        {
            // Créer le conteneur
            StackPanel content = new StackPanel
            {
                Margin = new Thickness(10)
            };

            // Déterminer le type de contenu en fonction du titre de la fenêtre
            string title = _targetWindow.Title.ToLower();

            // Simuler différents types de fenêtres
            if (title.Contains("paramètres") || title.Contains("settings") || title.Contains("options"))
            {
                // Simuler une fenêtre de paramètres
                content.Children.Add(CreateSettingsContent());
            }
            else if (title.Contains("rapport") || title.Contains("report") || title.Contains("résultats"))
            {
                // Simuler une fenêtre de rapport
                content.Children.Add(CreateReportContent());
            }
            else
            {
                // Contenu générique pour tout autre type de fenêtre
                content.Children.Add(CreateGenericContent());
            }

            return content;
        }

        /// <summary>
        /// Crée un contenu simulé pour une fenêtre de paramètres
        /// </summary>
        /// <returns>Un élément visuel représentant le contenu</returns>
        private UIElement CreateSettingsContent()
        {
            // Créer le conteneur
            Grid settingsGrid = new Grid();
            settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            settingsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            // Créer une liste de catégories
            ListView categories = new ListView
            {
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 10, 0)
            };

            // Ajouter quelques catégories
            categories.Items.Add("Général");
            categories.Items.Add("Affichage");
            categories.Items.Add("Notifications");
            categories.Items.Add("Confidentialité");
            categories.Items.Add("Mises à jour");
            categories.SelectedIndex = 0;

            // Créer un panel pour les options
            StackPanel options = new StackPanel
            {
                Margin = new Thickness(10, 0, 0, 0)
            };

            // Ajouter un titre
            TextBlock optionsTitle = new TextBlock
            {
                Text = "Paramètres généraux",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            options.Children.Add(optionsTitle);

            // Ajouter quelques options
            for (int i = 0; i < 4; i++)
            {
                DockPanel option = new DockPanel
                {
                    Margin = new Thickness(0, 5, 0, 5),
                    LastChildFill = true
                };

                CheckBox checkbox = new CheckBox
                {
                    IsChecked = i % 2 == 0,
                    VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(checkbox, Dock.Right);

                TextBlock label = new TextBlock
                {
                    Text = $"Option {i + 1}",
                    VerticalAlignment = VerticalAlignment.Center
                };

                option.Children.Add(checkbox);
                option.Children.Add(label);
                options.Children.Add(option);
            }

            // Ajouter un séparateur
            options.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });

            // Ajouter quelques sliders
            for (int i = 0; i < 2; i++)
            {
                TextBlock label = new TextBlock
                {
                    Text = $"Paramètre {i + 1}",
                    Margin = new Thickness(0, 5, 0, 0)
                };
                options.Children.Add(label);

                Slider slider = new Slider
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = 50,
                    Margin = new Thickness(0, 5, 0, 10)
                };
                options.Children.Add(slider);
            }

            // Ajouter les contrôles au grid
            Grid.SetColumn(categories, 0);
            Grid.SetColumn(options, 1);
            settingsGrid.Children.Add(categories);
            settingsGrid.Children.Add(options);

            return settingsGrid;
        }

        /// <summary>
        /// Crée un contenu simulé pour une fenêtre de rapport
        /// </summary>
        /// <returns>Un élément visuel représentant le contenu</returns>
        private UIElement CreateReportContent()
        {
            // Créer le conteneur
            StackPanel reportPanel = new StackPanel();

            // Ajouter un titre
            TextBlock title = new TextBlock
            {
                Text = "Rapport d'activité",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            };
            reportPanel.Children.Add(title);

            // Ajouter un sous-titre
            TextBlock subtitle = new TextBlock
            {
                Text = "Période: 01/04/2025 - 30/04/2025",
                FontSize = 14,
                FontStyle = FontStyles.Italic,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            reportPanel.Children.Add(subtitle);

            // Ajouter quelques statistiques
            Grid statsGrid = new Grid();
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid.RowDefinitions.Add(new RowDefinition());
            statsGrid.RowDefinitions.Add(new RowDefinition());

            // Ajouter quelques valeurs statistiques
            string[] statLabels = { "Total d'utilisateurs", "Sessions actives", "Temps moyen", "Taux de conversion", "Nouveaux visiteurs", "Pages vues" };
            string[] statValues = { "1,245", "324", "8m 12s", "24.5%", "456", "12,789" };

            for (int i = 0; i < 6; i++)
            {
                int row = i / 3;
                int col = i % 3;

                Border statContainer = new Border
                {
                    Margin = new Thickness(5),
                    Padding = new Thickness(10),
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    CornerRadius = new CornerRadius(5)
                };

                StackPanel statContent = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                TextBlock statValue = new TextBlock
                {
                    Text = statValues[i],
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                TextBlock statLabel = new TextBlock
                {
                    Text = statLabels[i],
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.DimGray),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                statContent.Children.Add(statValue);
                statContent.Children.Add(statLabel);
                statContainer.Child = statContent;

                Grid.SetRow(statContainer, row);
                Grid.SetColumn(statContainer, col);
                statsGrid.Children.Add(statContainer);
            }

            reportPanel.Children.Add(statsGrid);

            // Ajouter un séparateur
            reportPanel.Children.Add(new Separator { Margin = new Thickness(0, 20, 0, 20) });

            // Simuler un tableau
            Grid tableGrid = new Grid();
            tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Ajouter 4 colonnes
            for (int i = 0; i < 4; i++)
            {
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            // Ajouter l'en-tête du tableau
            string[] headers = { "Date", "Visiteurs", "Conversions", "Revenu" };
            for (int i = 0; i < headers.Length; i++)
            {
                Border headerCell = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    Padding = new Thickness(5),
                    BorderThickness = new Thickness(0, 0, i < headers.Length - 1 ? 1 : 0, 1),
                    BorderBrush = new SolidColorBrush(Colors.LightGray)
                };

                TextBlock headerText = new TextBlock
                {
                    Text = headers[i],
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                headerCell.Child = headerText;
                Grid.SetRow(headerCell, 0);
                Grid.SetColumn(headerCell, i);
                tableGrid.Children.Add(headerCell);
            }

            // Ajoutons 5 lignes de données
            string[] dates = { "01/04/2025", "05/04/2025", "10/04/2025", "15/04/2025", "20/04/2025" };
            int[] visitors = { 132, 198, 245, 187, 223 };
            int[] conversions = { 32, 47, 61, 43, 58 };
            string[] revenues = { "1,280 €", "1,880 €", "2,440 €", "1,720 €", "2,320 €" };

            for (int row = 0; row < 5; row++)
            {
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                for (int col = 0; col < 4; col++)
                {
                    Border cell = new Border
                    {
                        Padding = new Thickness(5),
                        BorderThickness = new Thickness(0, 0, col < 3 ? 1 : 0, 1),
                        BorderBrush = new SolidColorBrush(Colors.LightGray)
                    };

                    // Alternance des couleurs de ligne
                    if (row % 2 == 1)
                    {
                        cell.Background = new SolidColorBrush(Color.FromRgb(248, 248, 248));
                    }

                    string cellContent = "";
                    switch (col)
                    {
                        case 0: cellContent = dates[row]; break;
                        case 1: cellContent = visitors[row].ToString(); break;
                        case 2: cellContent = conversions[row].ToString(); break;
                        case 3: cellContent = revenues[row]; break;
                    }

                    TextBlock cellText = new TextBlock
                    {
                        Text = cellContent,
                        HorizontalAlignment = col == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right
                    };

                    cell.Child = cellText;
                    Grid.SetRow(cell, row + 1);
                    Grid.SetColumn(cell, col);
                    tableGrid.Children.Add(cell);
                }
            }

            reportPanel.Children.Add(tableGrid);

            return reportPanel;
        }

        /// <summary>
        /// Crée un contenu générique simulé pour tout autre type de fenêtre
        /// </summary>
        /// <returns>Un élément visuel représentant le contenu</returns>
        private UIElement CreateGenericContent()
        {
            // Créer le conteneur
            Grid genericGrid = new Grid();

            // Créer un menu
            DockPanel menuBar = new DockPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                Height = 25,
                LastChildFill = false
            };
            DockPanel.SetDock(menuBar, Dock.Top);

            // Ajouter quelques éléments de menu
            string[] menuItems = { "Fichier", "Édition", "Affichage", "Outils", "Aide" };
            foreach (string item in menuItems)
            {
                Border menuItem = new Border
                {
                    Padding = new Thickness(10, 0, 10, 0)
                };

                TextBlock menuText = new TextBlock
                {
                    Text = item,
                    VerticalAlignment = VerticalAlignment.Center
                };

                menuItem.Child = menuText;
                DockPanel.SetDock(menuItem, Dock.Left);
                menuBar.Children.Add(menuItem);
            }

            // Créer une section principale
            DockPanel mainSection = new DockPanel
            {
                LastChildFill = true,
                Margin = new Thickness(0, 5, 0, 0)
            };
            DockPanel.SetDock(mainSection, Dock.Top);

            // Créer une barre d'outils
            StackPanel toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245))
            };

            // Ajouter quelques boutons d'outils
            for (int i = 0; i < 5; i++)
            {
                Border toolButton = new Border
                {
                    Width = 30,
                    Height = 30,
                    Margin = new Thickness(2, 0, 2, 0)
                };

                Rectangle toolIcon = new Rectangle
                {
                    Width = 16,
                    Height = 16,
                    Fill = new SolidColorBrush(Colors.DimGray),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                toolButton.Child = toolIcon;
                toolbar.Children.Add(toolButton);
            }

            DockPanel.SetDock(toolbar, Dock.Top);
            mainSection.Children.Add(toolbar);

            // Créer un contenu principal
            ScrollViewer contentViewer = new ScrollViewer();
            StackPanel content = new StackPanel
            {
                Margin = new Thickness(10)
            };

            // Ajouter un titre
            TextBlock contentTitle = new TextBlock
            {
                Text = "Contenu principal",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            content.Children.Add(contentTitle);

            // Ajouter quelques paragraphes
            for (int i = 0; i < 3; i++)
            {
                TextBlock paragraph = new TextBlock
                {
                    Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed commodo, " +
                           "libero non consectetur vehicula, dui nunc tristique justo, at ultrices " +
                           "enim metus id enim. Vestibulum ante ipsum primis in faucibus.",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                content.Children.Add(paragraph);
            }

            contentViewer.Content = content;
            mainSection.Children.Add(contentViewer);

            // Créer une barre d'état
            Border statusBar = new Border
            {
                Height = 22,
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                BorderBrush = new SolidColorBrush(Colors.LightGray)
            };
            DockPanel.SetDock(statusBar, Dock.Bottom);

            TextBlock statusText = new TextBlock
            {
                Text = "Prêt",
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11
            };
            statusBar.Child = statusText;

            // Assembler tous les éléments
            genericGrid.Children.Add(menuBar);
            genericGrid.Children.Add(mainSection);
            genericGrid.Children.Add(statusBar);

            return genericGrid;
        }

        /// <summary>
        /// Met à jour le contenu simulé en fonction des nouvelles dimensions
        /// </summary>
        /// <param name="newSize">Nouvelles dimensions</param>
        private void UpdateSimulatedContent(Size newSize)
        {
            // Cette méthode peut être étendue pour ajuster le contenu simulé
            // en fonction des nouvelles dimensions
        }

        #endregion
    }
}