using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using HelloWorld;  // Pour accéder à ScreenUtility

namespace HelloWorld.Preview
{
    /// <summary>
    /// Gestionnaire central pour la prévisualisation de fenêtre.
    /// Cette classe coordonne tous les composants nécessaires pour créer, afficher
    /// et gérer la prévisualisation des dimensions d'une fenêtre.
    /// Version mise à jour pour utiliser l'implémentation consolidée de ScreenUtility.
    /// </summary>
    public class WindowPreviewManager : IWindowPreviewManager, IDisposable
    {
        #region Champs privés

        // Référence à la fenêtre cible dont on prévisualise les dimensions
        private Window _targetWindow;

        // Référence à la fenêtre de prévisualisation
        private PreviewWindow _previewWindow;

        // Fournisseur de dimensions à utiliser
        private IWindowDimensionProvider _dimensionProvider;

        // Renderer à utiliser pour la prévisualisation
        private IPreviewRenderer _previewRenderer;

        // Stratégie de positionnement à utiliser
        private IPositionStrategy _positionStrategy;

        // Timer pour limiter la fréquence des mises à jour
        private DispatcherTimer _updateThrottleTimer;

        // Dernière taille demandée pour mise à jour différée
        private Size? _pendingSize;

        // Indique si le gestionnaire est initialisé
        private bool _isInitialized;

        // Indique si une prévisualisation est en cours
        private bool _isPreviewActive;

        // Indique si le gestionnaire a été disposé
        private bool _isDisposed;

        // Dernières dimensions appliquées à la prévisualisation
        private Size _lastPreviewedSize;

        // Liste des moniteurs disponibles
        private List<HelloWorld.ScreenUtility.MonitorInfo> _availableMonitors;

        #endregion

        #region Propriétés publiques

        /// <summary>
        /// Obtient une valeur indiquant si une prévisualisation est actuellement active
        /// </summary>
        public bool IsPreviewActive => _isPreviewActive && !_isDisposed;

        /// <summary>
        /// Obtient une valeur indiquant si le gestionnaire a été correctement initialisé
        /// </summary>
        public bool IsInitialized => _isInitialized && !_isDisposed;

        /// <summary>
        /// Obtient les dernières dimensions prévisualisées
        /// </summary>
        public Size LastPreviewedSize => _lastPreviewedSize;

        #endregion

        #region Événements

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

        #endregion

        #region Constructeur et initialisation

        /// <summary>
        /// Initialise une nouvelle instance de la classe WindowPreviewManager
        /// </summary>
        public WindowPreviewManager()
        {
            // Initialiser les variables
            _isInitialized = false;
            _isPreviewActive = false;
            _isDisposed = false;
            _lastPreviewedSize = new Size(0, 0);

            // Créer le timer pour limiter la fréquence des mises à jour
            _updateThrottleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // 100ms entre les mises à jour
            };
            _updateThrottleTimer.Tick += UpdateThrottleTimer_Tick;

            // Définir la stratégie de positionnement par défaut (utilisant maintenant SmartPositionStrategy)
            _positionStrategy = new SmartPositionStrategy();

            // Initialiser la liste des moniteurs
            RefreshMonitorList();
        }

        /// <summary>
        /// Initialise le gestionnaire avec la fenêtre cible à prévisualiser
        /// </summary>
        /// <param name="targetWindow">Fenêtre cible dont on veut prévisualiser les dimensions</param>
        /// <exception cref="ArgumentNullException">Lancée si targetWindow est null</exception>
        /// <exception cref="InvalidOperationException">Lancée si le gestionnaire a déjà été initialisé</exception>
        public void Initialize(Window targetWindow)
        {
            // Vérifier que les paramètres sont valides
            if (targetWindow == null)
            {
                throw new ArgumentNullException(nameof(targetWindow), "La fenêtre cible ne peut pas être null");
            }

            // Vérifier que le gestionnaire n'a pas déjà été initialisé
            if (_isInitialized && !_isDisposed)
            {
                throw new InvalidOperationException("Le gestionnaire de prévisualisation a déjà été initialisé");
            }

            // Vérifier que le gestionnaire n'a pas été disposé
            EnsureNotDisposed();

            try
            {
                // Stocker la référence à la fenêtre cible
                _targetWindow = targetWindow;

                // S'abonner aux événements de la fenêtre cible
                _targetWindow.Closed += TargetWindow_Closed;
                _targetWindow.LocationChanged += TargetWindow_LocationChanged;

                // Initialiser le renderer par défaut si aucun n'a été défini
                if (_previewRenderer == null)
                {
                    _previewRenderer = new OutlinePreviewRenderer();
                }

                // Initialiser la stratégie de positionnement par défaut si aucune n'a été définie
                if (_positionStrategy == null)
                {
                    _positionStrategy = new SmartPositionStrategy();
                }

                // Initialiser le renderer avec la fenêtre cible
                _previewRenderer.Initialize(_targetWindow);

                // Marquer le gestionnaire comme initialisé
                _isInitialized = true;

                // Rafraîchir la liste des moniteurs
                RefreshMonitorList();
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                Debug.WriteLine($"Erreur lors de l'initialisation du gestionnaire de prévisualisation: {ex.Message}");

                // Nettoyer les ressources en cas d'erreur
                Cleanup();

                // Relancer l'exception
                throw new InvalidOperationException("Échec de l'initialisation du gestionnaire de prévisualisation", ex);
            }
        }

        #endregion

        #region Méthodes de gestion de la prévisualisation

        /// <summary>
        /// Démarre une session de prévisualisation avec les dimensions spécifiées
        /// </summary>
        /// <param name="newSize">Dimensions à prévisualiser</param>
        /// <exception cref="InvalidOperationException">Lancée si le gestionnaire n'a pas été initialisé</exception>
        public void StartPreview(Size newSize)
        {
            // Vérifier que le gestionnaire est initialisé
            EnsureInitialized();

            // Vérifier que le gestionnaire n'a pas été disposé
            EnsureNotDisposed();

            try
            {
                // Si une prévisualisation est déjà active, la mettre à jour
                if (_isPreviewActive)
                {
                    UpdatePreview(newSize);
                    return;
                }

                // Créer la fenêtre de prévisualisation si elle n'existe pas
                if (_previewWindow == null)
                {
                    _previewWindow = new PreviewWindow();
                    _previewWindow.Closed += PreviewWindow_Closed;
                }

                // Configurer la fenêtre de prévisualisation
                _previewWindow.SetRenderer(_previewRenderer);
                _previewWindow.SetTargetWindow(_targetWindow);

                // Mettre à jour les dimensions de la fenêtre de prévisualisation
                UpdatePreviewWindowSize(newSize);

                // Positionner la fenêtre de prévisualisation en utilisant la stratégie configurée
                PositionPreviewWindow(newSize);

                // Afficher la fenêtre de prévisualisation
                _previewWindow.Show();

                // Mettre à jour explicitement le renderer avec les dimensions du preview
                _previewRenderer.UpdateVisual(newSize);

                // Marquer la prévisualisation comme active
                _isPreviewActive = true;

                // Stocker les dernières dimensions prévisualisées
                _lastPreviewedSize = newSize;

                // Déclencher l'événement PreviewStarted
                OnPreviewStarted(new WindowDimensionEventArgs(newSize));
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors du démarrage de la prévisualisation: {ex.Message}");

                // Nettoyer la prévisualisation en cas d'erreur
                CleanupPreview();

                // Relancer l'exception
                throw new InvalidOperationException("Échec du démarrage de la prévisualisation", ex);
            }
        }

        /// <summary>
        /// Met à jour la prévisualisation avec de nouvelles dimensions
        /// </summary>
        /// <param name="newSize">Nouvelles dimensions à prévisualiser</param>
        /// <exception cref="InvalidOperationException">Lancée si aucune prévisualisation n'est active</exception>
        public void UpdatePreview(Size newSize)
        {
            // Vérifier que le gestionnaire est initialisé
            EnsureInitialized();

            // Vérifier que le gestionnaire n'a pas été disposé
            EnsureNotDisposed();

            // Vérifier que la prévisualisation est active
            if (!_isPreviewActive || _previewWindow == null)
            {
                throw new InvalidOperationException("Aucune prévisualisation n'est active");
            }

            try
            {
                // Stocker les dimensions pour une mise à jour différée
                _pendingSize = newSize;

                // Démarrer ou redémarrer le timer pour limiter la fréquence des mises à jour
                if (!_updateThrottleTimer.IsEnabled)
                {
                    _updateThrottleTimer.Start();
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                Debug.WriteLine($"Erreur lors de la mise à jour de la prévisualisation: {ex.Message}");

                // Relancer l'exception
                throw new InvalidOperationException("Échec de la mise à jour de la prévisualisation", ex);
            }
        }

        /// <summary>
        /// Arrête la session de prévisualisation en cours
        /// </summary>
        public void StopPreview()
        {
            // Vérifier que le gestionnaire n'a pas été disposé
            EnsureNotDisposed();

            // Si aucune prévisualisation n'est active, ne rien faire
            if (!_isPreviewActive)
            {
                return;
            }

            try
            {
                // Nettoyer la prévisualisation
                CleanupPreview();

                // Déclencher l'événement PreviewStopped
                OnPreviewStopped(new WindowDimensionEventArgs(_lastPreviewedSize));
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                Debug.WriteLine($"Erreur lors de l'arrêt de la prévisualisation: {ex.Message}");
            }
        }

        /// <summary>
        /// Applique les dimensions prévisualisées à la fenêtre cible
        /// </summary>
        /// <exception cref="InvalidOperationException">Lancée si aucune prévisualisation n'est active</exception>
        public void ApplyPreviewedDimensions()
        {
            // Vérifier que le gestionnaire est initialisé
            EnsureInitialized();

            // Vérifier que le gestionnaire n'a pas été disposé
            EnsureNotDisposed();

            // Si aucune prévisualisation n'est active, lever une exception
            if (!_isPreviewActive)
            {
                throw new InvalidOperationException("Aucune prévisualisation n'est active");
            }

            try
            {
                // Appliquer les dimensions à la fenêtre cible
                _targetWindow.Width = _lastPreviewedSize.Width;
                _targetWindow.Height = _lastPreviewedSize.Height;

                // Arrêter la prévisualisation
                StopPreview();

                // Déclencher l'événement PreviewApplied
                OnPreviewApplied(new WindowDimensionEventArgs(_lastPreviewedSize));
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                Debug.WriteLine($"Erreur lors de l'application des dimensions prévisualisées: {ex.Message}");

                // Relancer l'exception
                throw new InvalidOperationException("Échec de l'application des dimensions prévisualisées", ex);
            }
        }

        #endregion

        #region Méthodes de configuration

        /// <summary>
        /// Définit le fournisseur de dimensions à utiliser
        /// </summary>
        /// <param name="provider">Fournisseur de dimensions à utiliser</param>
        /// <exception cref="ArgumentNullException">Lancée si provider est null</exception>
        public void SetDimensionProvider(IWindowDimensionProvider provider)
        {
            // Vérifier que le paramètre est valide
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider), "Le fournisseur de dimensions ne peut pas être null");
            }

            // Vérifier que le gestionnaire n'a pas été disposé
            EnsureNotDisposed();

            // Se désabonner des événements de l'ancien fournisseur
            if (_dimensionProvider != null)
            {
                _dimensionProvider.DimensionsChanged -= DimensionProvider_DimensionsChanged;
            }

            // Stocker la référence au nouveau fournisseur
            _dimensionProvider = provider;

            // S'abonner aux événements du nouveau fournisseur
            _dimensionProvider.DimensionsChanged += DimensionProvider_DimensionsChanged;
        }

        /// <summary>
        /// Définit le renderer à utiliser pour la prévisualisation
        /// </summary>
        /// <param name="renderer">Renderer à utiliser</param>
        /// <exception cref="ArgumentNullException">Lancée si renderer est null</exception>
        public void SetPreviewRenderer(IPreviewRenderer renderer)
        {
            // Vérifier que le paramètre est valide
            if (renderer == null)
            {
                throw new ArgumentNullException(nameof(renderer), "Le renderer ne peut pas être null");
            }

            // Vérifier que le gestionnaire n'a pas été disposé
            EnsureNotDisposed();

            // Nettoyer l'ancien renderer
            if (_previewRenderer != null)
            {
                _previewRenderer.Cleanup();
            }

            // Stocker la référence au nouveau renderer
            _previewRenderer = renderer;

            // Initialiser le nouveau renderer si le gestionnaire est déjà initialisé
            if (_isInitialized && _targetWindow != null)
            {
                _previewRenderer.Initialize(_targetWindow);
            }

            // Mettre à jour la fenêtre de prévisualisation si elle existe
            if (_previewWindow != null)
            {
                _previewWindow.SetRenderer(_previewRenderer);
            }
        }

        /// <summary>
        /// Définit la stratégie de positionnement à utiliser
        /// </summary>
        /// <param name="strategy">Stratégie de positionnement à utiliser</param>
        /// <exception cref="ArgumentNullException">Lancée si strategy est null</exception>
        public void SetPositionStrategy(IPositionStrategy strategy)
        {
            // Vérifier que le paramètre est valide
            if (strategy == null)
            {
                throw new ArgumentNullException(nameof(strategy), "La stratégie de positionnement ne peut pas être null");
            }

            // Vérifier que le gestionnaire n'a pas été disposé
            EnsureNotDisposed();

            // Stocker la référence à la nouvelle stratégie
            _positionStrategy = strategy;

            // Mettre à jour la position de la fenêtre de prévisualisation si elle est active
            if (_isPreviewActive && _previewWindow != null)
            {
                PositionPreviewWindow(_lastPreviewedSize);
            }
        }

        #endregion

        #region Gestionnaires d'événements

        /// <summary>
        /// Gestionnaire de l'événement Tick du timer de limitation de fréquence
        /// </summary>
        private void UpdateThrottleTimer_Tick(object sender, EventArgs e)
        {
            // Arrêter le timer
            _updateThrottleTimer.Stop();

            // Si aucune taille n'est en attente, ne rien faire
            if (!_pendingSize.HasValue)
            {
                return;
            }

            try
            {
                // Récupérer la taille en attente
                Size newSize = _pendingSize.Value;
                _pendingSize = null;

                // Mettre à jour les dimensions de la fenêtre de prévisualisation
                UpdatePreviewWindowSize(newSize);

                // Positionner la fenêtre de prévisualisation
                PositionPreviewWindow(newSize);

                // Stocker les dernières dimensions prévisualisées
                _lastPreviewedSize = newSize;

                // Déclencher l'événement PreviewUpdated
                OnPreviewUpdated(new WindowDimensionEventArgs(newSize));
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                Debug.WriteLine($"Erreur lors de la mise à jour différée de la prévisualisation: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement DimensionsChanged du fournisseur de dimensions
        /// </summary>
        private void DimensionProvider_DimensionsChanged(object sender, WindowDimensionEventArgs e)
        {
            // Si une prévisualisation est active, la mettre à jour
            if (_isPreviewActive)
            {
                UpdatePreview(e.NewSize);
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement Closed de la fenêtre cible
        /// </summary>
        private void TargetWindow_Closed(object sender, EventArgs e)
        {
            // Nettoyer les ressources
            Dispose();
        }

        /// <summary>
        /// Gestionnaire de l'événement LocationChanged de la fenêtre cible
        /// </summary>
        private void TargetWindow_LocationChanged(object sender, EventArgs e)
        {
            // Si une prévisualisation est active, mettre à jour sa position
            if (_isPreviewActive && _previewWindow != null)
            {
                PositionPreviewWindow(_lastPreviewedSize);
            }
        }

        /// <summary>
        /// Gestionnaire de l'événement Closed de la fenêtre de prévisualisation
        /// </summary>
        /// <param name="sender">Objet qui a déclenché l'événement</param>
        /// <param name="e">Arguments de l'événement</param>
        private void PreviewWindow_Closed(object sender, EventArgs e)
        {
            // Marquer la prévisualisation comme inactive
            _isPreviewActive = false;

            // Nettoyer la référence à la fenêtre de prévisualisation
            _previewWindow = null;

            // Déclencher l'événement PreviewStopped
            OnPreviewStopped(new WindowDimensionEventArgs(_lastPreviewedSize));
        }

        #endregion

        #region Méthodes utilitaires privées

        /// <summary>
        /// Génère une liste de positions alternatives pour la fenêtre de prévisualisation
        /// Version améliorée avec plus d'options et priorisation intelligente
        /// </summary>
        /// <param name="targetRect">Rectangle de la fenêtre cible</param>
        /// <param name="previewSize">Taille de la fenêtre de prévisualisation</param>
        /// <returns>Tableau de positions alternatives à essayer</returns>
        private Point[] GetAlternativePositions(Rect targetRect, Size previewSize)
        {
            // Distance entre les fenêtres
            double margin = 10;

            // Liste des positions alternatives à essayer dans l'ordre de préférence
            return new Point[]
            {
        // À gauche de la fenêtre cible
        new Point(targetRect.Left - previewSize.Width - margin, targetRect.Top),
        
        // En dessous de la fenêtre cible
        new Point(targetRect.Left, targetRect.Bottom + margin),
        
        // Au-dessus de la fenêtre cible
        new Point(targetRect.Left, targetRect.Top - previewSize.Height - margin),
        
        // À droite de la fenêtre cible, aligné en bas
        new Point(targetRect.Right + margin, targetRect.Bottom - previewSize.Height),
        
        // À gauche de la fenêtre cible, aligné en bas
        new Point(targetRect.Left - previewSize.Width - margin, targetRect.Bottom - previewSize.Height),
        
        // En dessous de la fenêtre cible, aligné à droite
        new Point(targetRect.Right - previewSize.Width, targetRect.Bottom + margin),
        
        // Au-dessus de la fenêtre cible, aligné à droite
        new Point(targetRect.Right - previewSize.Width, targetRect.Top - previewSize.Height - margin),
        
        // En diagonale en bas à droite
        new Point(targetRect.Right + margin, targetRect.Bottom + margin),
        
        // En diagonale en bas à gauche
        new Point(targetRect.Left - previewSize.Width - margin, targetRect.Bottom + margin),
        
        // En diagonale en haut à droite
        new Point(targetRect.Right + margin, targetRect.Top - previewSize.Height - margin),
        
        // En diagonale en haut à gauche
        new Point(targetRect.Left - previewSize.Width - margin, targetRect.Top - previewSize.Height - margin)
            };
        }

        /// <summary>
        /// Classe pour représenter une position avec son score
        /// </summary>
        private class RankedPosition
        {
            public Point Position { get; set; }
            public double Score { get; set; }
        }

        /// <summary>
        /// Classe la liste des positions alternatives selon un score de pertinence
        /// prenant en compte la visibilité et la proximité avec la fenêtre cible
        /// </summary>
        /// <param name="positions">Positions à classer</param>
        /// <param name="targetRect">Rectangle de la fenêtre cible</param>
        /// <param name="previewSize">Taille de la fenêtre de prévisualisation</param>
        /// <returns>Liste ordonnée des positions (de la meilleure à la pire)</returns>
        private Point[] RankAlternativePositions(Point[] positions, Rect targetRect, Size previewSize)
        {
            try
            {
                var rankedPositions = new List<RankedPosition>();

                // Obtenir le centre de la fenêtre cible
                Point targetCenter = new Point(
                    targetRect.Left + targetRect.Width / 2,
                    targetRect.Top + targetRect.Height / 2);

                // Évaluer chaque position
                foreach (var position in positions)
                {
                    double score = 0;

                    // Créer un rectangle pour cette position
                    Rect posRect = new Rect(position, previewSize);

                    // Facteur de visibilité (le plus important)
                    double visibilityScore = 0;

                    foreach (var monitor in _availableMonitors)
                    {
                        Rect screenRect = new Rect(
                            monitor.Bounds.Left,
                            monitor.Bounds.Top,
                            monitor.Width,
                            monitor.Height);

                        if (screenRect.Contains(posRect))
                        {
                            // Entièrement visible - score maximal
                            visibilityScore = 1.0;
                            break;
                        }
                        else if (posRect.IntersectsWith(screenRect))
                        {
                            // Partiellement visible - score proportionnel
                            Rect intersection = Rect.Intersect(posRect, screenRect);
                            double rectArea = posRect.Width * posRect.Height;
                            double visibleArea = intersection.Width * intersection.Height;
                            double currentScore = visibleArea / rectArea;

                            visibilityScore = Math.Max(visibilityScore, currentScore);
                        }
                    }

                    // Facteur de proximité (moins important)
                    double distance = Math.Sqrt(
                        Math.Pow(targetCenter.X - (position.X + previewSize.Width / 2), 2) +
                        Math.Pow(targetCenter.Y - (position.Y + previewSize.Height / 2), 2));

                    // Normaliser la distance (plus la distance est petite, plus le score est élevé)
                    double maxDistance = 1000; // Distance de référence
                    double proximityScore = 1.0 - Math.Min(1.0, distance / maxDistance);

                    // Score final: 80% visibilité, 20% proximité
                    score = (visibilityScore * 0.8) + (proximityScore * 0.2);

                    // Ajouter à la liste
                    rankedPositions.Add(new RankedPosition { Position = position, Score = score });
                }

                // Trier par score décroissant et retourner les positions
                return rankedPositions
                    .OrderByDescending(p => p.Score)
                    .Select(p => p.Position)
                    .ToArray();
            }
            catch (Exception ex)
            {
                // En cas d'erreur, retourner les positions non triées
                System.Diagnostics.Debug.WriteLine($"Erreur lors du classement des positions: {ex.Message}");
                return positions;
            }
        }

        /// <summary>
        /// Obtient le facteur d'échelle DPI pour un moniteur
        /// </summary>
        /// <param name="monitor">Moniteur pour lequel obtenir le facteur d'échelle</param>
        /// <returns>Facteur d'échelle DPI (1.0 = 100%)</returns>
        private double GetDpiScaleFactorForMonitor(HelloWorld.ScreenUtility.MonitorInfo monitor)
        {
            return WindowPositioningHelper.GetDpiScaleFactor(monitor);
        }

        /// <summary>
        /// Met à jour les dimensions de la fenêtre de prévisualisation
        /// </summary>
        /// <param name="newSize">Nouvelles dimensions à appliquer</param>
        private void UpdatePreviewWindowSize(Size newSize)
        {
            // Vérifier que la fenêtre de prévisualisation existe
            if (_previewWindow == null)
            {
                System.Diagnostics.Debug.WriteLine("UpdatePreviewWindowSize: _previewWindow est null");
                return;
            }

            // Vérifier que le renderer existe
            if (_previewRenderer == null)
            {
                System.Diagnostics.Debug.WriteLine("UpdatePreviewWindowSize: _previewRenderer est null");
                return;
            }

            try
            {
                // Vérifier que les dimensions sont valides
                if (newSize.Width <= 0 || newSize.Height <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdatePreviewWindowSize: Dimensions invalides: {newSize.Width}x{newSize.Height}");
                    return;
                }

                // Journaliser les dimensions pour faciliter le débogage
                System.Diagnostics.Debug.WriteLine($"WindowPreviewManager: Mise à jour du preview avec dimensions {newSize.Width}x{newSize.Height}");

                // Mettre à jour les dimensions de la fenêtre de prévisualisation
                _previewWindow.Width = newSize.Width;
                _previewWindow.Height = newSize.Height;

                // IMPORTANT: Mettre à jour le renderer AVANT que la fenêtre ne soit visible
                // pour s'assurer que les dimensions affichées sont correctes dès le début
                _previewRenderer.UpdateVisual(newSize);

                // Mettre à jour explicitement les dimensions dans le ViewModel de la fenêtre
                if (_previewWindow.ViewModel != null)
                {
                    _previewWindow.ViewModel.UpdateDimensions((int)newSize.Width, (int)newSize.Height);
                    _previewWindow.ViewModel.ShowDimensions = true;
                }
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur pour le débogage
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la mise à jour des dimensions du preview: {ex.Message}");
            }
        }

        /// <summary>
        /// Positionne la fenêtre de prévisualisation en utilisant la stratégie de positionnement
        /// et en tenant compte correctement du DPI et du scaling
        /// Version améliorée avec gestion robuste des erreurs et cas limites
        /// </summary>
        /// <param name="newSize">Dimensions de la fenêtre de prévisualisation</param>
        private void PositionPreviewWindow(Size newSize)
        {
            // Vérifier que la fenêtre de prévisualisation existe
            if (_previewWindow == null || _targetWindow == null)
            {
                return;
            }

            try
            {
                // Calculer la position de la fenêtre de prévisualisation
                Point targetPosition = new Point(_targetWindow.Left, _targetWindow.Top);
                Size targetSize = new Size(_targetWindow.Width, _targetWindow.Height);

                // Créer un rectangle pour la fenêtre cible
                Rect targetRect = new Rect(targetPosition, targetSize);

                // Utiliser la stratégie de positionnement pour calculer la position
                Point previewPosition;

                // Vérifier que la stratégie de positionnement n'est pas null
                if (_positionStrategy == null)
                {
                    // Si aucune stratégie n'est définie, utiliser SnapPositionStrategy par défaut
                    _positionStrategy = new SnapPositionStrategy();
                    System.Diagnostics.Debug.WriteLine("PositionPreviewWindow: Stratégie manquante, création d'une SnapPositionStrategy par défaut");
                }

                // IMPORTANT: Utiliser systématiquement la méthode CalculatePosition de la stratégie
                previewPosition = _positionStrategy.CalculatePosition(
                    targetPosition, targetSize, newSize, _availableMonitors);

                // Journaliser pour le débogage
                System.Diagnostics.Debug.WriteLine($"Position calculée par la stratégie: X={previewPosition.X}, Y={previewPosition.Y}");

                // Créer un rectangle pour la position calculée
                Rect previewRect = new Rect(previewPosition, newSize);

                // Vérifier si la fenêtre est entièrement visible sur un écran
                bool isVisible = WindowPositioningHelper.IsRectanglePartiallyVisible(previewRect, 0.8);

                // AJOUT IMPORTANT: Si la fenêtre principale est proche du bord droit de l'écran
                // et qu'aucune position ne semble viable, forcer la position à gauche
                if (!isVisible)
                {
                    // Vérifier si la fenêtre principale est proche du bord droit
                    foreach (var monitor in _availableMonitors)
                    {
                        // Créer un rectangle pour l'écran
                        Rect screenRect = new Rect(
                            monitor.Bounds.Left,
                            monitor.Bounds.Top,
                            monitor.Width,
                            monitor.Height);

                        // Si la fenêtre est sur cet écran
                        if (screenRect.IntersectsWith(targetRect))
                        {
                            // Vérifier la distance au bord droit
                            double distanceToRightEdge = screenRect.Right - targetRect.Right;
                            if (distanceToRightEdge < monitor.Width * 0.20) // À moins de 20% du bord droit
                            {
                                System.Diagnostics.Debug.WriteLine("Fenêtre détectée près du bord droit - Position forcée à gauche");

                                // Forcer la position à gauche
                                previewPosition = new Point(
                                    targetRect.Left - newSize.Width - 10, // 10 pixels de marge
                                    targetRect.Top);                      // Même hauteur

                                // Recréer le rectangle avec cette position
                                previewRect = new Rect(previewPosition, newSize);

                                // Si cette position est au moins partiellement visible, l'utiliser
                                if (WindowPositioningHelper.IsRectanglePartiallyVisible(previewRect, 0.3))
                                {
                                    isVisible = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                // Si la fenêtre n'est pas visible ou est trop peu visible, essayer d'autres positions
                if (!isVisible)
                {
                    System.Diagnostics.Debug.WriteLine("Position calculée non visible, essai de positions alternatives");

                    // Essayer différentes positions alternatives
                    Point[] alternativePositions = GetAlternativePositions(targetRect, newSize);

                    foreach (Point position in alternativePositions)
                    {
                        Rect altRect = new Rect(position, newSize);
                        if (WindowPositioningHelper.IsRectanglePartiallyVisible(altRect, 0.8))
                        {
                            previewPosition = position;
                            isVisible = true;
                            System.Diagnostics.Debug.WriteLine($"Position alternative trouvée: X={previewPosition.X}, Y={previewPosition.Y}");
                            break;
                        }
                    }
                }

                // Si toujours pas visible, centrer sur l'écran qui contient la fenêtre cible
                if (!isVisible)
                {
                    System.Diagnostics.Debug.WriteLine("Aucune position viable trouvée, centrage sur l'écran");

                    // Trouver l'écran qui contient la fenêtre cible
                    HelloWorld.ScreenUtility.MonitorInfo targetMonitor = WindowPositioningHelper.FindMonitorContainingWindow(_targetWindow);

                    // Si aucun écran ne contient la fenêtre cible, utiliser l'écran principal
                    if (targetMonitor == null)
                    {
                        targetMonitor = HelloWorld.ScreenUtility.PrimaryMonitor;
                    }

                    // Si un écran a été trouvé, centrer la fenêtre de prévisualisation sur cet écran
                    if (targetMonitor != null)
                    {
                        previewPosition = new Point(
                            targetMonitor.Bounds.Left + (targetMonitor.Width - newSize.Width) / 2,
                            targetMonitor.Bounds.Top + (targetMonitor.Height - newSize.Height) / 2);

                        System.Diagnostics.Debug.WriteLine($"Centrage sur écran: X={previewPosition.X}, Y={previewPosition.Y}");
                    }
                }

                // Mise à jour finale - contraindre aux limites des écrans pour s'assurer que la fenêtre reste visible
                Rect finalRect = new Rect(previewPosition, newSize);
                Rect constrainedRect = WindowPositioningHelper.ConstrainRectToScreen(finalRect);

                // Appliquer la position à la fenêtre de prévisualisation
                _previewWindow.Left = constrainedRect.Left;
                _previewWindow.Top = constrainedRect.Top;

                System.Diagnostics.Debug.WriteLine($"Position finale après contrainte: X={constrainedRect.Left}, Y={constrainedRect.Top}");
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors du positionnement de la fenêtre de prévisualisation: {ex.Message}");

                // En cas d'erreur, utiliser une position par défaut sécurisée
                try
                {
                    // Trouver l'écran qui contient la fenêtre cible
                    HelloWorld.ScreenUtility.MonitorInfo targetMonitor = WindowPositioningHelper.FindMonitorContainingWindow(_targetWindow);

                    // Si aucun écran ne contient la fenêtre cible, utiliser l'écran principal
                    if (targetMonitor == null)
                    {
                        targetMonitor = HelloWorld.ScreenUtility.PrimaryMonitor;
                    }

                    // Si un écran a été trouvé, placer la fenêtre au centre
                    if (targetMonitor != null)
                    {
                        _previewWindow.Left = targetMonitor.Bounds.Left + (targetMonitor.Width - newSize.Width) / 2;
                        _previewWindow.Top = targetMonitor.Bounds.Top + (targetMonitor.Height - newSize.Height) / 2;
                    }
                    else
                    {
                        // En dernier recours, position relative à la fenêtre cible
                        _previewWindow.Left = _targetWindow.Left + _targetWindow.Width / 2 - newSize.Width / 2;
                        _previewWindow.Top = _targetWindow.Top + _targetWindow.Height / 2 - newSize.Height / 2;
                    }
                }
                catch
                {
                    // Si tout échoue, utiliser une position absolue simple
                    _previewWindow.Left = 100;
                    _previewWindow.Top = 100;
                }
            }
        }

        /// <summary>
        /// Rafraîchit la liste des moniteurs disponibles en utilisant les 
        /// API Windows pour obtenir des informations précises et actualisées
        /// </summary>
        private void RefreshMonitorList()
        {
            try
            {
                // Utiliser directement la propriété statique Monitors de ScreenUtility
                _availableMonitors = new List<HelloWorld.ScreenUtility.MonitorInfo>(HelloWorld.ScreenUtility.Monitors);
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                Debug.WriteLine($"Erreur lors du rafraîchissement de la liste des moniteurs: {ex.Message}");

                // Initialiser une liste vide en cas d'erreur
                _availableMonitors = new List<HelloWorld.ScreenUtility.MonitorInfo>();
            }
        }

        /// <summary>
        /// Nettoie les ressources de la prévisualisation
        /// </summary>
        private void CleanupPreview()
        {
            // Arrêter le timer de mise à jour
            if (_updateThrottleTimer.IsEnabled)
            {
                _updateThrottleTimer.Stop();
            }

            // Effacer les dimensions en attente
            _pendingSize = null;

            // Fermer la fenêtre de prévisualisation
            if (_previewWindow != null)
            {
                // Se désabonner de l'événement Closed pour éviter les appels récursifs
                _previewWindow.Closed -= PreviewWindow_Closed;

                // Fermer la fenêtre
                _previewWindow.Close();
                _previewWindow = null;
            }

            // Marquer la prévisualisation comme inactive
            _isPreviewActive = false;
        }

        /// <summary>
        /// Nettoie toutes les ressources utilisées par le gestionnaire
        /// </summary>
        private void Cleanup()
        {
            // Nettoyer la prévisualisation
            CleanupPreview();

            // Se désabonner des événements de la fenêtre cible
            if (_targetWindow != null)
            {
                _targetWindow.Closed -= TargetWindow_Closed;
                _targetWindow.LocationChanged -= TargetWindow_LocationChanged;
                _targetWindow = null;
            }

            // Se désabonner des événements du fournisseur de dimensions
            if (_dimensionProvider != null)
            {
                _dimensionProvider.DimensionsChanged -= DimensionProvider_DimensionsChanged;
                _dimensionProvider = null;
            }

            // Nettoyer le renderer
            if (_previewRenderer != null)
            {
                _previewRenderer.Cleanup();
                _previewRenderer = null;
            }

            // Réinitialiser les indicateurs d'état
            _isInitialized = false;
            _isPreviewActive = false;
        }

        /// <summary>
        /// Vérifie que le gestionnaire est initialisé et lance une exception sinon
        /// </summary>
        /// <exception cref="InvalidOperationException">Lancée si le gestionnaire n'est pas initialisé</exception>
        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Le gestionnaire de prévisualisation n'a pas été initialisé");
            }
        }

        /// <summary>
        /// Vérifie que le gestionnaire n'a pas été disposé et lance une exception sinon
        /// </summary>
        /// <exception cref="ObjectDisposedException">Lancée si le gestionnaire a été disposé</exception>
        private void EnsureNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name, "Le gestionnaire de prévisualisation a été disposé");
            }
        }

        #endregion

        #region Méthodes d'événements

        /// <summary>
        /// Déclenche l'événement PreviewStarted
        /// </summary>
        /// <param name="e">Arguments de l'événement</param>
        protected virtual void OnPreviewStarted(WindowDimensionEventArgs e)
        {
            PreviewStarted?.Invoke(this, e);
        }

        /// <summary>
        /// Déclenche l'événement PreviewUpdated
        /// </summary>
        /// <param name="e">Arguments de l'événement</param>
        protected virtual void OnPreviewUpdated(WindowDimensionEventArgs e)
        {
            PreviewUpdated?.Invoke(this, e);
        }

        /// <summary>
        /// Déclenche l'événement PreviewStopped
        /// </summary>
        /// <param name="e">Arguments de l'événement</param>
        protected virtual void OnPreviewStopped(WindowDimensionEventArgs e)
        {
            PreviewStopped?.Invoke(this, e);
        }

        /// <summary>
        /// Déclenche l'événement PreviewApplied
        /// </summary>
        /// <param name="e">Arguments de l'événement</param>
        protected virtual void OnPreviewApplied(WindowDimensionEventArgs e)
        {
            PreviewApplied?.Invoke(this, e);
        }

        #endregion

        #region Implémentation de IDisposable

        /// <summary>
        /// Libère les ressources utilisées par le gestionnaire
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Libère les ressources utilisées par le gestionnaire
        /// </summary>
        /// <param name="disposing">Indique si la méthode a été appelée depuis Dispose() (true) ou depuis le finaliseur (false)</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                // Libérer les ressources managées
                Cleanup();

                // Arrêter le timer
                if (_updateThrottleTimer != null)
                {
                    _updateThrottleTimer.Stop();
                    _updateThrottleTimer.Tick -= UpdateThrottleTimer_Tick;
                    _updateThrottleTimer = null;
                }
            }

            // Libérer les ressources non managées (aucune dans ce cas)

            // Marquer l'objet comme disposé
            _isDisposed = true;
        }

        /// <summary>
        /// Destructeur
        /// </summary>
        ~WindowPreviewManager()
        {
            Dispose(false);
        }

        #endregion
    }
}