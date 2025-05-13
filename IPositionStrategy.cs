using System.Collections.Generic;
using System.Windows;

namespace HelloWorld.Preview
{
    /// <summary>
    /// Interface définissant une stratégie de positionnement pour la fenêtre de prévisualisation.
    /// Les implémentations de cette interface sont utilisées par WindowPreviewManager pour 
    /// déterminer où placer la fenêtre de prévisualisation par rapport à la fenêtre cible.
    /// </summary>
    public interface IPositionStrategy
    {
        /// <summary>
        /// Calcule la position où la fenêtre de prévisualisation devrait être placée
        /// </summary>
        /// <param name="originalWindowPosition">Position actuelle de la fenêtre principale</param>
        /// <param name="originalWindowSize">Taille actuelle de la fenêtre principale</param>
        /// <param name="previewWindowSize">Taille prévue pour la fenêtre de prévisualisation</param>
        /// <param name="screens">Informations sur les écrans disponibles</param>
        /// <returns>Point indiquant où positionner la fenêtre de prévisualisation</returns>
        Point CalculatePosition(Point originalWindowPosition, Size originalWindowSize,
                               Size previewWindowSize, IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens);
    }

    /// <summary>
    /// Stratégie de positionnement composite qui permet d'utiliser plusieurs stratégies
    /// avec une logique de priorité ou de conditions pour choisir la stratégie à appliquer.
    /// </summary>
    public class CompositePositionStrategy : IPositionStrategy
    {
        private readonly List<IPositionStrategy> _strategies = new List<IPositionStrategy>();
        private Condition _condition;

        /// <summary>
        /// Représente une condition pour choisir entre les stratégies
        /// </summary>
        /// <param name="originalWindowPosition">Position actuelle de la fenêtre principale</param>
        /// <param name="originalWindowSize">Taille actuelle de la fenêtre principale</param>
        /// <param name="previewWindowSize">Taille prévue pour la fenêtre de prévisualisation</param>
        /// <param name="screens">Informations sur les écrans disponibles</param>
        /// <returns>Indice de la stratégie à utiliser</returns>
        public delegate int Condition(Point originalWindowPosition, Size originalWindowSize,
                                     Size previewWindowSize, IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens);

        /// <summary>
        /// Initialise une nouvelle instance de CompositePositionStrategy avec une condition
        /// </summary>
        /// <param name="condition">Condition pour choisir la stratégie</param>
        /// <param name="strategies">Stratégies à utiliser</param>
        public CompositePositionStrategy(Condition condition, params IPositionStrategy[] strategies)
        {
            _condition = condition;

            if (strategies != null)
            {
                _strategies.AddRange(strategies);
            }
        }

        /// <summary>
        /// Initialise une nouvelle instance de CompositePositionStrategy qui utilisera
        /// toujours la première stratégie qui réussit (retourne une position visible)
        /// </summary>
        /// <param name="strategies">Stratégies à utiliser par ordre de priorité</param>
        public CompositePositionStrategy(params IPositionStrategy[] strategies)
        {
            // Condition par défaut: toujours utiliser la première stratégie
            _condition = (p, os, ps, screens) => 0;

            if (strategies != null)
            {
                _strategies.AddRange(strategies);
            }
        }

        /// <summary>
        /// Ajoute une stratégie à la liste des stratégies disponibles
        /// </summary>
        /// <param name="strategy">Stratégie à ajouter</param>
        public void AddStrategy(IPositionStrategy strategy)
        {
            if (strategy != null)
            {
                _strategies.Add(strategy);
            }
        }

        /// <summary>
        /// Calcule la position où la fenêtre de prévisualisation devrait être placée
        /// en utilisant la stratégie choisie par la condition
        /// </summary>
        /// <param name="originalWindowPosition">Position actuelle de la fenêtre principale</param>
        /// <param name="originalWindowSize">Taille actuelle de la fenêtre principale</param>
        /// <param name="previewWindowSize">Taille prévue pour la fenêtre de prévisualisation</param>
        /// <param name="screens">Informations sur les écrans disponibles</param>
        /// <returns>Point indiquant où positionner la fenêtre de prévisualisation</returns>
        public Point CalculatePosition(Point originalWindowPosition, Size originalWindowSize,
                                      Size previewWindowSize, IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens)
        {
            // Vérifier qu'il y a au moins une stratégie
            if (_strategies.Count == 0)
            {
                // Position par défaut (même position que la fenêtre originale)
                return originalWindowPosition;
            }

            // Obtenir l'indice de la stratégie à utiliser
            int strategyIndex = _condition(originalWindowPosition, originalWindowSize, previewWindowSize, screens);

            // S'assurer que l'indice est valide
            strategyIndex = System.Math.Max(0, System.Math.Min(_strategies.Count - 1, strategyIndex));

            // Utiliser la stratégie choisie
            return _strategies[strategyIndex].CalculatePosition(
                originalWindowPosition, originalWindowSize, previewWindowSize, screens);
        }
    }

    /// <summary>
    /// Stratégie de positionnement intelligente qui mémorise la dernière position relative
    /// de la fenêtre de prévisualisation par rapport à la fenêtre principale.
    /// </summary>
    public class MemoryPositionStrategy : IPositionStrategy
    {
        private IPositionStrategy _defaultStrategy;
        private Point? _lastRelativePosition;

        /// <summary>
        /// Initialise une nouvelle instance de MemoryPositionStrategy
        /// </summary>
        /// <param name="defaultStrategy">Stratégie par défaut à utiliser si aucune position n'a été mémorisée</param>
        public MemoryPositionStrategy(IPositionStrategy defaultStrategy)
        {
            _defaultStrategy = defaultStrategy ?? new AdjacentPositionStrategy();
        }

        /// <summary>
        /// Calcule la position où la fenêtre de prévisualisation devrait être placée
        /// en utilisant la dernière position relative mémorisée si disponible
        /// </summary>
        /// <param name="originalWindowPosition">Position actuelle de la fenêtre principale</param>
        /// <param name="originalWindowSize">Taille actuelle de la fenêtre principale</param>
        /// <param name="previewWindowSize">Taille prévue pour la fenêtre de prévisualisation</param>
        /// <param name="screens">Informations sur les écrans disponibles</param>
        /// <returns>Point indiquant où positionner la fenêtre de prévisualisation</returns>
        public Point CalculatePosition(Point originalWindowPosition, Size originalWindowSize,
                                      Size previewWindowSize, IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens)
        {
            // Si aucune position relative n'a été mémorisée, utiliser la stratégie par défaut
            if (!_lastRelativePosition.HasValue)
            {
                Point newPosition = _defaultStrategy.CalculatePosition(
                    originalWindowPosition, originalWindowSize, previewWindowSize, screens);

                // Mémoriser la position relative
                _lastRelativePosition = new Point(
                    newPosition.X - originalWindowPosition.X,
                    newPosition.Y - originalWindowPosition.Y);

                return newPosition;
            }

            // Calculer la nouvelle position absolue basée sur la position relative mémorisée
            Point calculatedPosition = new Point(
                originalWindowPosition.X + _lastRelativePosition.Value.X,
                originalWindowPosition.Y + _lastRelativePosition.Value.Y);

            // Vérifier si cette position est visible sur l'écran
            if (IsVisibleOnScreen(calculatedPosition, previewWindowSize, screens))
            {
                return calculatedPosition;
            }

            // Si la position calculée n'est pas visible, utiliser la stratégie par défaut
            Point adjustedPosition = _defaultStrategy.CalculatePosition(
                originalWindowPosition, originalWindowSize, previewWindowSize, screens);

            // Mettre à jour la position relative mémorisée
            _lastRelativePosition = new Point(
                adjustedPosition.X - originalWindowPosition.X,
                adjustedPosition.Y - originalWindowPosition.Y);

            return adjustedPosition;
        }

        /// <summary>
        /// Réinitialise la position mémorisée
        /// </summary>
        public void ResetMemory()
        {
            _lastRelativePosition = null;
        }

        /// <summary>
        /// Définit explicitement la position relative à mémoriser
        /// </summary>
        /// <param name="relativePosition">Position relative à mémoriser</param>
        public void SetRelativePosition(Point relativePosition)
        {
            _lastRelativePosition = relativePosition;
        }

        /// <summary>
        /// Vérifie si une position est visible sur l'un des écrans disponibles
        /// </summary>
        /// <param name="position">Position à vérifier</param>
        /// <param name="size">Taille de la fenêtre</param>
        /// <param name="screens">Informations sur les écrans disponibles</param>
        /// <returns>True si la position est visible, sinon False</returns>
        private bool IsVisibleOnScreen(Point position, Size size, IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens)
        {
            if (screens == null)
                return true; // Si aucune information d'écran n'est disponible, on suppose que c'est visible

            // Créer un rectangle représentant la fenêtre
            Rect windowRect = new Rect(position, size);

            foreach (var screen in screens)
            {
                // Convertir les structures RECT en Rect de WPF
                Rect screenRect = new Rect(
                    screen.Bounds.Left,
                    screen.Bounds.Top,
                    screen.Width,
                    screen.Height);

                // Vérifier si une partie significative (au moins 50%) du rectangle est visible
                if (windowRect.IntersectsWith(screenRect))
                {
                    Rect intersection = Rect.Intersect(windowRect, screenRect);
                    double visibleArea = intersection.Width * intersection.Height;
                    double totalArea = windowRect.Width * windowRect.Height;

                    if (visibleArea >= totalArea * 0.5)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Implémentation d'IPositionStrategy qui applique une contrainte de visibilité 
    /// à une autre stratégie de positionnement.
    /// </summary>
    public class ConstrainedPositionStrategy : IPositionStrategy
    {
        private IPositionStrategy _baseStrategy;
        private IPositionStrategy _fallbackStrategy;

        /// <summary>
        /// Initialise une nouvelle instance de ConstrainedPositionStrategy
        /// </summary>
        /// <param name="baseStrategy">Stratégie de base à utiliser</param>
        /// <param name="fallbackStrategy">Stratégie de secours si la position calculée n'est pas visible</param>
        public ConstrainedPositionStrategy(IPositionStrategy baseStrategy, IPositionStrategy fallbackStrategy = null)
        {
            _baseStrategy = baseStrategy ?? throw new System.ArgumentNullException(nameof(baseStrategy));
            _fallbackStrategy = fallbackStrategy ?? new CenterScreenPositionStrategy();
        }

        /// <summary>
        /// Calcule la position où la fenêtre de prévisualisation devrait être placée
        /// en s'assurant qu'elle reste visible sur l'écran
        /// </summary>
        /// <param name="originalWindowPosition">Position actuelle de la fenêtre principale</param>
        /// <param name="originalWindowSize">Taille actuelle de la fenêtre principale</param>   
        /// <param name="previewWindowSize">Taille prévue pour la fenêtre de prévisualisation</param>
        /// <param name="screens">Informations sur les écrans disponibles</param>
        /// <returns>Point indiquant où positionner la fenêtre de prévisualisation</returns>
        public Point CalculatePosition(Point originalWindowPosition, Size originalWindowSize,
                                      Size previewWindowSize, IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens)
        {
            // Calculer la position selon la stratégie de base
            Point basePosition = _baseStrategy.CalculatePosition(
                originalWindowPosition, originalWindowSize, previewWindowSize, screens);

            // Créer un rectangle pour la position calculée
            Rect baseRect = new Rect(basePosition, previewWindowSize);

            // Vérifier si cette position est visible sur l'écran en utilisant la méthode centralisée
            if (ScreenUtility.IsRectangleFullyVisible(baseRect))
            {
                return basePosition;
            }

            // Si la position n'est pas entièrement visible, essayer de l'ajuster
            Point adjustedPosition = AdjustToFitScreen(basePosition, previewWindowSize, screens);

            // Créer un rectangle pour la position ajustée
            Rect adjustedRect = new Rect(adjustedPosition, previewWindowSize);

            // Vérifier si cette position est visible sur l'écran en utilisant la méthode centralisée
            if (ScreenUtility.IsRectangleFullyVisible(adjustedRect))
            {
                return adjustedPosition;
            }

            // En dernier recours, utiliser la stratégie de secours
            return _fallbackStrategy.CalculatePosition(
                originalWindowPosition, originalWindowSize, previewWindowSize, screens);
        }

        /// <summary>
        /// Ajuste une position pour qu'elle soit entièrement visible sur l'écran
        /// </summary>
        /// <param name="position">Position à ajuster</param>
        /// <param name="size">Taille de la fenêtre</param>
        /// <param name="screens">Informations sur les écrans disponibles</param>
        /// <returns>Position ajustée</returns>
        private Point AdjustToFitScreen(Point position, Size size, IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens)
        {
            if (screens == null)
                return position;

            // Créer un rectangle représentant la fenêtre
            Rect windowRect = new Rect(position, size);

            // Trouver l'écran qui contient le centre de la fenêtre
            Point windowCenter = new Point(
                position.X + size.Width / 2,
                position.Y + size.Height / 2);

            HelloWorld.ScreenUtility.MonitorInfo targetScreen = null;

            foreach (var screen in screens)
            {
                // Convertir les structures RECT en Rect de WPF
                Rect screenRect = new Rect(
                    screen.Bounds.Left,
                    screen.Bounds.Top,
                    screen.Width,
                    screen.Height);

                // Vérifier si l'écran contient le centre de la fenêtre
                if (screenRect.Contains(windowCenter))
                {
                    targetScreen = screen;
                    break;
                }
            }

            // Si aucun écran ne contient le centre, prendre le premier écran
            if (targetScreen == null && screens.GetEnumerator().MoveNext())
            {
                targetScreen = screens.GetEnumerator().Current;
            }

            // Si toujours aucun écran, retourner la position inchangée
            if (targetScreen == null)
                return position;

            // Créer un rectangle représentant l'écran
            Rect screenRect2 = new Rect(
                targetScreen.Bounds.Left,
                targetScreen.Bounds.Top,
                targetScreen.Width,
                targetScreen.Height);

            // Ajuster la position pour que la fenêtre soit entièrement visible
            double adjustedX = position.X;
            double adjustedY = position.Y;

            // Ajuster horizontalement
            if (position.X < screenRect2.Left)
            {
                adjustedX = screenRect2.Left;
            }
            else if (position.X + size.Width > screenRect2.Right)
            {
                adjustedX = screenRect2.Right - size.Width;
            }

            // Ajuster verticalement
            if (position.Y < screenRect2.Top)
            {
                adjustedY = screenRect2.Top;
            }
            else if (position.Y + size.Height > screenRect2.Bottom)
            {
                adjustedY = screenRect2.Bottom - size.Height;
            }

            return new Point(adjustedX, adjustedY);
        }
    }

    /// <summary>
    /// Stratégie de positionnement qui calcule la position optimale en fonction
    /// des zones d'ancrage (dock) définies autour de la fenêtre principale.
    /// </summary>
    public class DockPositionStrategy : IPositionStrategy
    {
        // Zones d'ancrage possibles
        public enum DockZone
        {
            Right,
            Left,
            Bottom,
            Top,
            BottomRight,
            BottomLeft,
            TopRight,
            TopLeft
        }

        private DockZone _preferredZone;
        private DockZone[] _fallbackZones;
        private int _margin;

        /// <summary>
        /// Initialise une nouvelle instance de DockPositionStrategy
        /// </summary>
        /// <param name="preferredZone">Zone d'ancrage préférée</param>
        /// <param name="margin">Marge en pixels entre les fenêtres</param>
        /// <param name="fallbackZones">Zones d'ancrage alternatives par ordre de préférence</param>
        public DockPositionStrategy(DockZone preferredZone = DockZone.Right, int margin = 10,
                                    DockZone[] fallbackZones = null)
        {
            _preferredZone = preferredZone;
            _margin = margin;

            // Zones de repli par défaut dans un ordre logique
            if (fallbackZones == null)
            {
                _fallbackZones = new DockZone[]
                {
                    DockZone.Right,
                    DockZone.Bottom,
                    DockZone.Left,
                    DockZone.Top,
                    DockZone.BottomRight,
                    DockZone.BottomLeft,
                    DockZone.TopRight,
                    DockZone.TopLeft
                };

                // Enlever la zone préférée de la liste des zones de repli
                System.Collections.Generic.List<DockZone> zones =
                    new System.Collections.Generic.List<DockZone>(_fallbackZones);
                zones.Remove(_preferredZone);
                _fallbackZones = zones.ToArray();
            }
            else
            {
                _fallbackZones = fallbackZones;
            }
        }

        /// <summary>
        /// Calcule la position où la fenêtre de prévisualisation devrait être placée
        /// en fonction de la zone d'ancrage préférée
        /// </summary>
        /// <param name="originalWindowPosition">Position actuelle de la fenêtre principale</param>
        /// <param name="originalWindowSize">Taille actuelle de la fenêtre principale</param>
        /// <param name="previewWindowSize">Taille prévue pour la fenêtre de prévisualisation</param>
        /// <param name="screens">Informations sur les écrans disponibles</param>
        /// <returns>Point indiquant où positionner la fenêtre de prévisualisation</returns>
        public Point CalculatePosition(Point originalWindowPosition, Size originalWindowSize,
                                      Size previewWindowSize, IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens)
        {
            // Essayer d'abord la zone préférée
            Point position = CalculatePositionForZone(_preferredZone, originalWindowPosition,
                                                     originalWindowSize, previewWindowSize);

            // Vérifier si cette position est visible
            if (IsVisibleOnScreen(position, previewWindowSize, screens))
            {
                return position;
            }

            // Essayer les zones de repli
            foreach (DockZone zone in _fallbackZones)
            {
                position = CalculatePositionForZone(zone, originalWindowPosition,
                                                   originalWindowSize, previewWindowSize);

                if (IsVisibleOnScreen(position, previewWindowSize, screens))
                {
                    return position;
                }
            }

            // En dernier recours, centrer sur l'écran qui contient la fenêtre originale
            HelloWorld.ScreenUtility.MonitorInfo targetScreen = FindScreenContainingWindow(
                originalWindowPosition, originalWindowSize, screens);

            if (targetScreen != null)
            {
                return new Point(
                    targetScreen.Bounds.Left + (targetScreen.Width - previewWindowSize.Width) / 2,
                    targetScreen.Bounds.Top + (targetScreen.Height - previewWindowSize.Height) / 2);
            }

            // Si aucun écran n'est trouvé, utiliser la position par défaut
            return CalculatePositionForZone(_preferredZone, originalWindowPosition,
                                           originalWindowSize, previewWindowSize);
        }

        /// <summary>
        /// Calcule la position pour une zone d'ancrage spécifique
        /// </summary>
        /// <param name="zone">Zone d'ancrage</param>
        /// <param name="originalWindowPosition">Position de la fenêtre principale</param>
        /// <param name="originalWindowSize">Taille de la fenêtre principale</param>
        /// <param name="previewWindowSize">Taille de la fenêtre de prévisualisation</param>
        /// <returns>Position calculée</returns>
        private Point CalculatePositionForZone(DockZone zone, Point originalWindowPosition,
                                              Size originalWindowSize, Size previewWindowSize)
        {
            switch (zone)
            {
                case DockZone.Right:
                    return new Point(
                        originalWindowPosition.X + originalWindowSize.Width + _margin,
                        originalWindowPosition.Y);

                case DockZone.Left:
                    return new Point(
                        originalWindowPosition.X - previewWindowSize.Width - _margin,
                        originalWindowPosition.Y);

                case DockZone.Bottom:
                    return new Point(
                        originalWindowPosition.X,
                        originalWindowPosition.Y + originalWindowSize.Height + _margin);

                case DockZone.Top:
                    return new Point(
                        originalWindowPosition.X,
                        originalWindowPosition.Y - previewWindowSize.Height - _margin);

                case DockZone.BottomRight:
                    return new Point(
                        originalWindowPosition.X + originalWindowSize.Width + _margin,
                        originalWindowPosition.Y + originalWindowSize.Height + _margin);

                case DockZone.BottomLeft:
                    return new Point(
                        originalWindowPosition.X - previewWindowSize.Width - _margin,
                        originalWindowPosition.Y + originalWindowSize.Height + _margin);

                case DockZone.TopRight:
                    return new Point(
                        originalWindowPosition.X + originalWindowSize.Width + _margin,
                        originalWindowPosition.Y - previewWindowSize.Height - _margin);

                case DockZone.TopLeft:
                    return new Point(
                        originalWindowPosition.X - previewWindowSize.Width - _margin,
                        originalWindowPosition.Y - previewWindowSize.Height - _margin);

                default:
                    return new Point(
                        originalWindowPosition.X + originalWindowSize.Width + _margin,
                        originalWindowPosition.Y);
            }
        }

        /// <summary>
        /// Vérifie si une position est visible sur l'un des écrans disponibles
        /// </summary>
        /// <param name="position">Position à vérifier</param>
        /// <param name="size">Taille de la fenêtre</param>
        /// <param name="screens">Informations sur les écrans disponibles</param>
        /// <returns>True si la position est visible, sinon False</returns>
        private bool IsVisibleOnScreen(Point position, Size size, IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens)
        {
            if (screens == null)
                return true; // Si aucune information d'écran n'est disponible, on suppose que c'est visible

            // Créer un rectangle représentant la fenêtre
            Rect windowRect = new Rect(position, size);

            foreach (var screen in screens)
            {
                // Convertir les structures RECT en Rect de WPF
                Rect screenRect = new Rect(
                    screen.Bounds.Left,
                    screen.Bounds.Top,
                    screen.Width,
                    screen.Height);

                // Vérifier si le rectangle est au moins partiellement visible sur cet écran
                if (windowRect.IntersectsWith(screenRect))
                {
                    // Vérifier si une partie significative (au moins 80%) du rectangle est visible
                    Rect intersection = Rect.Intersect(windowRect, screenRect);
                    double visibleArea = intersection.Width * intersection.Height;
                    double totalArea = windowRect.Width * windowRect.Height;

                    if (visibleArea >= totalArea * 0.8)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Trouve l'écran qui contient la fenêtre principale
        /// </summary>
        /// <param name="windowPosition">Position de la fenêtre</param>
        /// <param name="windowSize">Taille de la fenêtre</param>
        /// <param name="screens">Liste des écrans disponibles</param>
        /// <returns>Information sur l'écran contenant la fenêtre, ou null si aucun écran ne contient la fenêtre</returns>
        private HelloWorld.ScreenUtility.MonitorInfo FindScreenContainingWindow(Point windowPosition, Size windowSize,
                                                                    IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens)
        {
            if (screens == null)
                return null;

            // Créer un rectangle représentant la fenêtre
            Rect windowRect = new Rect(windowPosition, windowSize);

            // Rechercher l'écran qui contient le plus grand pourcentage de la fenêtre
            HelloWorld.ScreenUtility.MonitorInfo bestMatch = null;
            double largestIntersection = 0;

            foreach (var screen in screens)
            {
                // Convertir la structure RECT en Rect de WPF
                Rect screenRect = new Rect(
                    screen.Bounds.Left,
                    screen.Bounds.Top,
                    screen.Width,
                    screen.Height);

                // Calculer l'intersection
                if (windowRect.IntersectsWith(screenRect))
                {
                    Rect intersection = Rect.Intersect(windowRect, screenRect);
                    double area = intersection.Width * intersection.Height;

                    if (area > largestIntersection)
                    {
                        largestIntersection = area;
                        bestMatch = screen;
                    }
                }
            }

            // Si aucun écran ne contient la fenêtre, retourner l'écran principal
            if (bestMatch == null)
            {
                foreach (var screen in screens)
                {
                    if (screen.IsPrimary)
                    {
                        bestMatch = screen;
                        break;
                    }
                }
            }

            // Si toujours aucun écran trouvé, retourner le premier écran
            if (bestMatch == null)
            {
                var enumerator = screens.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    bestMatch = enumerator.Current;
                }
            }

            return bestMatch;
        }
    }
}