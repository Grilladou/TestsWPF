using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HelloWorld
{
    /// <summary>
    /// Classe utilitaire qui fournit des méthodes pour le positionnement de fenêtres
    /// et la gestion des coordonnées en tenant compte du DPI et des multiples écrans.
    /// Cette classe est conçue selon le principe de responsabilité unique (SRP)
    /// pour séparer la logique de positionnement de la collecte d'informations sur les écrans.
    /// </summary>
    public static class WindowPositioningHelper
    {
        #region Constantes et membres privés

        /// <summary>
        /// Marge par défaut en pixels pour le positionnement des fenêtres
        /// </summary>
        private const int DEFAULT_MARGIN = 10;

        /// <summary>
        /// Pourcentage minimal de visibilité par défaut pour qu'une fenêtre soit considérée comme visible
        /// </summary>
        private const double DEFAULT_VISIBILITY_THRESHOLD = 0.8;

        /// <summary>
        /// DPI de référence pour Windows (96 est la valeur standard)
        /// </summary>
        private const double REFERENCE_DPI = 96.0;

        #endregion

        #region Énumérations et types

        /// <summary>
        /// Énumération des directions pour le positionnement
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// Vers la gauche
            /// </summary>
            Left,

            /// <summary>
            /// Vers la droite
            /// </summary>
            Right,

            /// <summary>
            /// Vers le haut
            /// </summary>
            Up,

            /// <summary>
            /// Vers le bas
            /// </summary>
            Down
        }

        /// <summary>
        /// Types de distribution des fenêtres sur les écrans multiples
        /// </summary>
        public enum MonitorDistribution
        {
            /// <summary>
            /// Toutes les fenêtres sur l'écran principal
            /// </summary>
            Primary,

            /// <summary>
            /// Fenêtres réparties également sur tous les écrans
            /// </summary>
            Equally,

            /// <summary>
            /// Première fenêtre sur l'écran principal, les autres sur les écrans secondaires
            /// </summary>
            MainSecondary,

            /// <summary>
            /// Distribution personnalisée (à définir par l'utilisateur)
            /// </summary>
            Custom
        }

        #endregion

        #region Conversion DPI et coordonnées

        /// <summary>
        /// Obtient le facteur d'échelle DPI pour un moniteur donné
        /// </summary>
        /// <param name="monitor">Moniteur dont on veut obtenir le facteur d'échelle</param>
        /// <returns>Facteur d'échelle DPI (1.0 = 96 DPI)</returns>
        public static double GetDpiScaleFactor(ScreenUtility.MonitorInfo monitor)
        {
            // Vérifier que le moniteur n'est pas null
            if (monitor == null)
                return 1.0;

            // Calculer le facteur d'échelle en fonction du DPI
            return monitor.DpiX / REFERENCE_DPI;
        }

        /// <summary>
        /// Convertit des coordonnées logiques en coordonnées physiques
        /// </summary>
        /// <param name="logicalPoint">Point en coordonnées logiques</param>
        /// <param name="monitor">Moniteur de référence</param>
        /// <returns>Point en coordonnées physiques</returns>
        public static Point LogicalToPhysical(Point logicalPoint, ScreenUtility.MonitorInfo monitor)
        {
            // Vérifier que le moniteur n'est pas null
            if (monitor == null)
                return logicalPoint;

            // Obtenir le facteur d'échelle
            double dpiScale = GetDpiScaleFactor(monitor);

            // Appliquer le facteur d'échelle
            return new Point(
                logicalPoint.X * dpiScale,
                logicalPoint.Y * dpiScale);
        }

        /// <summary>
        /// Convertit des coordonnées physiques en coordonnées logiques
        /// </summary>
        /// <param name="physicalPoint">Point en coordonnées physiques</param>
        /// <param name="monitor">Moniteur de référence</param>
        /// <returns>Point en coordonnées logiques</returns>
        public static Point PhysicalToLogical(Point physicalPoint, ScreenUtility.MonitorInfo monitor)
        {
            // Vérifier que le moniteur n'est pas null
            if (monitor == null)
                return physicalPoint;

            // Obtenir le facteur d'échelle
            double dpiScale = GetDpiScaleFactor(monitor);

            // Vérifier que le facteur d'échelle n'est pas nul ou négatif pour éviter une division par zéro
            if (dpiScale <= 0.0)
                return physicalPoint;

            // Appliquer l'inverse du facteur d'échelle
            return new Point(
                physicalPoint.X / dpiScale,
                physicalPoint.Y / dpiScale);
        }

        /// <summary>
        /// Convertit un rectangle en coordonnées logiques en coordonnées physiques
        /// </summary>
        /// <param name="logicalRect">Rectangle en coordonnées logiques</param>
        /// <param name="monitor">Moniteur de référence</param>
        /// <returns>Rectangle en coordonnées physiques</returns>
        public static Rect LogicalToPhysical(Rect logicalRect, ScreenUtility.MonitorInfo monitor)
        {
            // Vérifier que le moniteur n'est pas null
            if (monitor == null)
                return logicalRect;

            // Obtenir le facteur d'échelle
            double dpiScale = GetDpiScaleFactor(monitor);

            // Appliquer le facteur d'échelle
            return new Rect(
                logicalRect.X * dpiScale,
                logicalRect.Y * dpiScale,
                logicalRect.Width * dpiScale,
                logicalRect.Height * dpiScale);
        }

        /// <summary>
        /// Convertit un rectangle en coordonnées physiques en coordonnées logiques
        /// </summary>
        /// <param name="physicalRect">Rectangle en coordonnées physiques</param>
        /// <param name="monitor">Moniteur de référence</param>
        /// <returns>Rectangle en coordonnées logiques</returns>
        public static Rect PhysicalToLogical(Rect physicalRect, ScreenUtility.MonitorInfo monitor)
        {
            // Vérifier que le moniteur n'est pas null
            if (monitor == null)
                return physicalRect;

            // Obtenir le facteur d'échelle
            double dpiScale = GetDpiScaleFactor(monitor);

            // Vérifier que le facteur d'échelle n'est pas nul ou négatif pour éviter une division par zéro
            if (dpiScale <= 0.0)
                return physicalRect;

            // Appliquer l'inverse du facteur d'échelle
            return new Rect(
                physicalRect.X / dpiScale,
                physicalRect.Y / dpiScale,
                physicalRect.Width / dpiScale,
                physicalRect.Height / dpiScale);
        }

        /// <summary>
        /// Convertit un rectangle de moniteur en coordonnées logiques
        /// </summary>
        /// <param name="monitor">Moniteur à convertir</param>
        /// <returns>Rectangle en coordonnées logiques représentant le moniteur</returns>
        public static Rect MonitorToLogicalRect(ScreenUtility.MonitorInfo monitor)
        {
            // Vérifier que le moniteur n'est pas null
            if (monitor == null)
                return Rect.Empty;

            // Créer un rectangle en coordonnées physiques
            Rect physicalRect = new Rect(
                monitor.Bounds.Left,
                monitor.Bounds.Top,
                monitor.Width,
                monitor.Height);

            // Convertir en coordonnées logiques
            return PhysicalToLogical(physicalRect, monitor);
        }

        #endregion

        #region Détection de visibilité et d'écrans

        /// <summary>
        /// Trouve l'écran qui contient un point spécifique
        /// </summary>
        /// <param name="point">Point à tester</param>
        /// <returns>Écran contenant le point, ou null si aucun écran ne contient le point</returns>
        public static ScreenUtility.MonitorInfo FindMonitorContainingPoint(Point point)
        {
            // Obtenir la liste des moniteurs
            var monitors = ScreenUtility.Monitors;

            // Vérifier chaque moniteur pour voir s'il contient le point
            foreach (var monitor in monitors)
            {
                // Vérifier si le point est dans les limites du moniteur
                if (point.X >= monitor.Bounds.Left && point.X < monitor.Bounds.Right &&
                    point.Y >= monitor.Bounds.Top && point.Y < monitor.Bounds.Bottom)
                {
                    return monitor;
                }
            }

            // Si aucun moniteur ne contient le point, retourner null
            return null;
        }

        /// <summary>
        /// Trouve l'écran qui contient une fenêtre
        /// </summary>
        /// <param name="window">Fenêtre à tester</param>
        /// <returns>Écran contenant la fenêtre, ou null si aucun écran ne contient la fenêtre</returns>
        public static ScreenUtility.MonitorInfo FindMonitorContainingWindow(Window window)
        {
            // Vérifier que la fenêtre n'est pas null
            if (window == null)
                return null;

            // Obtenir le centre de la fenêtre
            Point center = new Point(
                window.Left + window.Width / 2,
                window.Top + window.Height / 2);

            return FindMonitorContainingPoint(center);
        }

        /// <summary>
        /// Trouve l'écran qui contient la plus grande partie d'un rectangle
        /// </summary>
        /// <param name="rect">Rectangle à tester</param>
        /// <returns>Moniteur contenant la plus grande partie du rectangle, ou null si aucun moniteur ne contient le rectangle</returns>
        public static ScreenUtility.MonitorInfo FindBestMonitorForRect(Rect rect)
        {
            // Obtenir la liste des moniteurs
            var monitors = ScreenUtility.Monitors;

            // Vérifier que la liste d'écrans n'est pas vide
            if (monitors == null || !monitors.Any())
                return null;

            // Rechercher l'écran qui contient le plus grand pourcentage de la fenêtre
            ScreenUtility.MonitorInfo bestMatch = null;
            double largestIntersection = 0;

            foreach (var monitor in monitors)
            {
                // Créer un rectangle représentant l'écran
                Rect monitorRect = new Rect(
                    monitor.Bounds.Left,
                    monitor.Bounds.Top,
                    monitor.Width,
                    monitor.Height);

                // Calculer l'intersection
                if (rect.IntersectsWith(monitorRect))
                {
                    Rect intersection = Rect.Intersect(rect, monitorRect);
                    double area = intersection.Width * intersection.Height;

                    if (area > largestIntersection)
                    {
                        largestIntersection = area;
                        bestMatch = monitor;
                    }
                }
            }

            // Si aucun moniteur ne contient le rectangle, essayer de trouver le moniteur le plus proche
            if (bestMatch == null)
            {
                double bestDistance = double.MaxValue;

                // Calculer le centre du rectangle
                Point rectCenter = new Point(
                    rect.Left + rect.Width / 2,
                    rect.Top + rect.Height / 2);

                foreach (var monitor in monitors)
                {
                    // Calculer le centre du moniteur
                    Point monitorCenter = new Point(
                        monitor.Bounds.Left + monitor.Width / 2,
                        monitor.Bounds.Top + monitor.Height / 2);

                    // Calculer la distance entre les centres
                    double distance = Math.Sqrt(
                        Math.Pow(rectCenter.X - monitorCenter.X, 2) +
                        Math.Pow(rectCenter.Y - monitorCenter.Y, 2));

                    // Si cette distance est plus petite que la meilleure trouvée jusqu'à présent, l'enregistrer
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestMatch = monitor;
                    }
                }
            }

            return bestMatch;
        }

        /// <summary>
        /// Vérifie si un rectangle est entièrement visible sur un écran
        /// </summary>
        /// <param name="rect">Rectangle à tester</param>
        /// <returns>True si le rectangle est entièrement visible, sinon False</returns>
        public static bool IsRectangleFullyVisible(Rect rect)
        {
            // Obtenir la liste des moniteurs
            var monitors = ScreenUtility.Monitors;

            // Vérifier chaque moniteur
            foreach (var monitor in monitors)
            {
                // Créer un rectangle représentant l'écran
                Rect monitorRect = new Rect(
                    monitor.Bounds.Left,
                    monitor.Bounds.Top,
                    monitor.Width,
                    monitor.Height);

                // Vérifier si le rectangle est entièrement contenu dans l'écran
                if (monitorRect.Contains(rect))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Vérifie si un rectangle est partiellement visible sur un écran
        /// </summary>
        /// <param name="rect">Rectangle à tester</param>
        /// <param name="minVisiblePercent">Pourcentage minimal visible (0.0 - 1.0)</param>
        /// <returns>True si le rectangle est partiellement visible, sinon False</returns>
        public static bool IsRectanglePartiallyVisible(Rect rect, double minVisiblePercent = DEFAULT_VISIBILITY_THRESHOLD)
        {
            // Obtenir la liste des moniteurs
            var monitors = ScreenUtility.Monitors;

            // Vérifier que les paramètres sont valides
            double rectArea = rect.Width * rect.Height;
            if (rectArea <= 0)
                return false;

            // Limiter le pourcentage entre 0 et 1
            minVisiblePercent = Math.Max(0.0, Math.Min(1.0, minVisiblePercent));

            // Vérifier chaque moniteur
            foreach (var monitor in monitors)
            {
                // Créer un rectangle représentant l'écran
                Rect monitorRect = new Rect(
                    monitor.Bounds.Left,
                    monitor.Bounds.Top,
                    monitor.Width,
                    monitor.Height);

                // Si le rectangle est entièrement contenu, retourner vrai immédiatement
                if (monitorRect.Contains(rect))
                {
                    return true;
                }

                // Vérifier l'intersection
                if (rect.IntersectsWith(monitorRect))
                {
                    // Calculer la zone d'intersection
                    Rect intersection = Rect.Intersect(rect, monitorRect);
                    double visibleArea = intersection.Width * intersection.Height;

                    // Vérifier si la partie visible dépasse le seuil minimal
                    if (visibleArea >= rectArea * minVisiblePercent)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Détermine si une position est visible sur un écran avec un niveau de confiance minimal
        /// </summary>
        /// <param name="position">Position à vérifier</param>
        /// <param name="minimumConfidence">Niveau de confiance minimal (0.0-1.0)</param>
        /// <returns>True si la position est visible avec le niveau de confiance spécifié</returns>
        public static bool IsPositionVisibleOnAnyScreen(Point position, double minimumConfidence = 0.5)
        {
            // Obtenir la liste des moniteurs
            var monitors = ScreenUtility.Monitors;

            // Vérifier chaque moniteur
            foreach (var monitor in monitors)
            {
                // Créer un rectangle représentant l'écran
                Rect screenRect = new Rect(
                    monitor.Bounds.Left,
                    monitor.Bounds.Top,
                    monitor.Width,
                    monitor.Height);

                // Vérifier si la position est contenue dans l'écran
                if (screenRect.Contains(position))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Positionnement de fenêtres

        /// <summary>
        /// Positionne une fenêtre au centre d'un moniteur spécifique
        /// </summary>
        /// <param name="window">Fenêtre à positionner</param>
        /// <param name="monitor">Moniteur où centrer la fenêtre</param>
        public static void CenterWindowOnMonitor(Window window, ScreenUtility.MonitorInfo monitor)
        {
            // Vérifier que les paramètres ne sont pas null
            if (window == null || monitor == null)
                return;

            // Calculer le facteur d'échelle DPI
            double dpiScaleFactor = GetDpiScaleFactor(monitor);

            // Calculer les coordonnées centrées en tenant compte du DPI
            double left = monitor.Bounds.Left + (monitor.Width - (window.Width * dpiScaleFactor)) / 2;
            double top = monitor.Bounds.Top + (monitor.Height - (window.Height * dpiScaleFactor)) / 2;

            // Appliquer les nouvelles coordonnées en tenant compte du DPI
            window.Left = left / dpiScaleFactor;
            window.Top = top / dpiScaleFactor;
        }

        /// <summary>
        /// Ajuste la position d'une fenêtre pour qu'elle soit entièrement visible sur le moniteur
        /// </summary>
        /// <param name="window">Fenêtre à ajuster</param>
        /// <param name="monitor">Moniteur de référence (ou null pour utiliser le moniteur contenant la fenêtre)</param>
        public static void EnsureWindowVisibility(Window window, ScreenUtility.MonitorInfo monitor = null)
        {
            // Vérifier que la fenêtre n'est pas null
            if (window == null)
                return;

            // Si aucun moniteur n'est spécifié, rechercher le moniteur contenant la fenêtre
            if (monitor == null)
            {
                monitor = FindMonitorContainingWindow(window);

                // Si aucun moniteur ne contient la fenêtre, utiliser le moniteur principal
                if (monitor == null)
                {
                    monitor = ScreenUtility.PrimaryMonitor;

                    // Si toujours pas de moniteur, sortir
                    if (monitor == null)
                        return;
                }
            }

            try
            {
                // Calculer le facteur d'échelle DPI
                double dpiScaleFactor = GetDpiScaleFactor(monitor);

                // Calculer les dimensions de la fenêtre en coordonnées écran
                double windowWidth = window.Width * dpiScaleFactor;
                double windowHeight = window.Height * dpiScaleFactor;
                double windowLeft = window.Left * dpiScaleFactor;
                double windowTop = window.Top * dpiScaleFactor;

                // Vérifier et ajuster la position horizontale
                if (windowLeft < monitor.Bounds.Left)
                {
                    windowLeft = monitor.Bounds.Left;
                }
                else if (windowLeft + windowWidth > monitor.Bounds.Right)
                {
                    windowLeft = monitor.Bounds.Right - windowWidth;
                }

                // Vérifier et ajuster la position verticale
                if (windowTop < monitor.Bounds.Top)
                {
                    windowTop = monitor.Bounds.Top;
                }
                else if (windowTop + windowHeight > monitor.Bounds.Bottom)
                {
                    windowTop = monitor.Bounds.Bottom - windowHeight;
                }

                // Reconvertir les coordonnées écran en coordonnées fenêtre
                window.Left = windowLeft / dpiScaleFactor;
                window.Top = windowTop / dpiScaleFactor;
            }
            catch (Exception ex)
            {
                // Journal d'erreur
                Debug.WriteLine($"Erreur lors de l'ajustement de la visibilité de la fenêtre: {ex.Message}");

                // En cas d'erreur, tenter une approche plus simple
                try
                {
                    // Centrer la fenêtre sur le moniteur comme solution de secours
                    CenterWindowOnMonitor(window, monitor);
                }
                catch
                {
                    // Ignorer toute erreur supplémentaire
                }
            }
        }

        /// <summary>
        /// Contraint un rectangle pour qu'il soit entièrement visible sur un écran
        /// </summary>
        /// <param name="rect">Rectangle à contraindre</param>
        /// <returns>Rectangle contraint qui est entièrement visible sur un écran</returns>
        public static Rect ConstrainRectToScreen(Rect rect)
        {
            // Si le rectangle est déjà entièrement visible, le retourner tel quel
            if (IsRectangleFullyVisible(rect))
                return rect;

            try
            {
                // Trouver le moniteur qui contient la plus grande partie du rectangle
                var bestMonitor = FindBestMonitorForRect(rect);
                if (bestMonitor == null)
                {
                    // Si aucun moniteur ne contient le rectangle, utiliser le moniteur principal
                    bestMonitor = ScreenUtility.PrimaryMonitor;

                    // Si toujours pas de moniteur, retourner le rectangle tel quel
                    if (bestMonitor == null)
                        return rect;
                }

                // Créer un rectangle représentant l'écran
                Rect screenRect = new Rect(
                    bestMonitor.Bounds.Left,
                    bestMonitor.Bounds.Top,
                    bestMonitor.Width,
                    bestMonitor.Height);

                // Ajuster le rectangle pour qu'il soit entièrement visible sur l'écran
                double left = rect.Left;
                double top = rect.Top;
                double width = rect.Width;
                double height = rect.Height;

                // Ajuster la largeur et la hauteur si elles dépassent la taille de l'écran
                if (width > screenRect.Width)
                    width = screenRect.Width;

                if (height > screenRect.Height)
                    height = screenRect.Height;

                // Ajuster la position horizontale
                if (left < screenRect.Left)
                {
                    left = screenRect.Left;
                }
                else if (left + width > screenRect.Right)
                {
                    left = screenRect.Right - width;
                }

                // Ajuster la position verticale
                if (top < screenRect.Top)
                {
                    top = screenRect.Top;
                }
                else if (top + height > screenRect.Bottom)
                {
                    top = screenRect.Bottom - height;
                }

                // Retourner le rectangle ajusté
                return new Rect(left, top, width, height);
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                Debug.WriteLine($"Erreur lors de la contrainte du rectangle: {ex.Message}");

                // En cas d'erreur, retourner le rectangle original
                return rect;
            }
        }

        /// <summary>
        /// Déplace une fenêtre vers un moniteur adjacent dans la direction spécifiée
        /// </summary>
        /// <param name="window">Fenêtre à déplacer</param>
        /// <param name="direction">Direction du déplacement</param>
        /// <returns>True si la fenêtre a été déplacée, False si aucun moniteur adjacent n'a été trouvé</returns>
        public static bool MoveWindowToAdjacentMonitor(Window window, Direction direction)
        {
            // Vérifier que la fenêtre n'est pas null
            if (window == null)
                return false;

            try
            {
                // Rechercher le moniteur contenant la fenêtre
                var currentMonitor = FindMonitorContainingWindow(window);
                if (currentMonitor == null)
                    return false;

                // Rechercher le moniteur adjacent dans la direction spécifiée
                var adjacentMonitor = FindAdjacentMonitor(currentMonitor, direction);
                if (adjacentMonitor == null)
                    return false;

                // Calculer la position relative de la fenêtre dans le moniteur actuel
                double relX = (window.Left * GetDpiScaleFactor(currentMonitor) - currentMonitor.Bounds.Left) / currentMonitor.Width;
                double relY = (window.Top * GetDpiScaleFactor(currentMonitor) - currentMonitor.Bounds.Top) / currentMonitor.Height;

                // Limiter les positions relatives entre 0 et 1
                relX = Math.Max(0, Math.Min(1, relX));
                relY = Math.Max(0, Math.Min(1, relY));

                // Calculer la nouvelle position dans le moniteur adjacent
                double newX = adjacentMonitor.Bounds.Left + (adjacentMonitor.Width * relX);
                double newY = adjacentMonitor.Bounds.Top + (adjacentMonitor.Height * relY);

                // Convertir les coordonnées écran en coordonnées fenêtre
                window.Left = newX / GetDpiScaleFactor(adjacentMonitor);
                window.Top = newY / GetDpiScaleFactor(adjacentMonitor);

                // S'assurer que la fenêtre est visible sur le nouveau moniteur
                EnsureWindowVisibility(window, adjacentMonitor);

                return true;
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                Debug.WriteLine($"Erreur lors du déplacement de la fenêtre: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Trouve un moniteur adjacent à un moniteur donné dans la direction spécifiée
        /// </summary>
        /// <param name="monitor">Moniteur de référence</param>
        /// <param name="direction">Direction de recherche</param>
        /// <returns>Moniteur adjacent ou null si aucun n'est trouvé</returns>
        private static ScreenUtility.MonitorInfo FindAdjacentMonitor(ScreenUtility.MonitorInfo monitor, Direction direction)
        {
            // Vérifier que le moniteur n'est pas null
            if (monitor == null)
                return null;

            try
            {
                // Obtenir tous les moniteurs
                var monitors = ScreenUtility.Monitors;

                // S'assurer que la liste n'est pas vide ou null
                if (monitors == null || !monitors.Any())
                    return null;

                // Filtrer les moniteurs selon la direction
                IEnumerable<ScreenUtility.MonitorInfo> candidates = null;
                switch (direction)
                {
                    case Direction.Left:
                        candidates = monitors.Where(m => m.Bounds.Right <= monitor.Bounds.Left);
                        break;

                    case Direction.Right:
                        candidates = monitors.Where(m => m.Bounds.Left >= monitor.Bounds.Right);
                        break;

                    case Direction.Up:
                        candidates = monitors.Where(m => m.Bounds.Bottom <= monitor.Bounds.Top);
                        break;

                    case Direction.Down:
                        candidates = monitors.Where(m => m.Bounds.Top >= monitor.Bounds.Bottom);
                        break;

                    default:
                        return null;
                }

                // S'il n'y a pas de candidats, retourner null
                if (candidates == null || !candidates.Any())
                    return null;

                // Calculer les scores de chaque candidat
                var scores = new Dictionary<ScreenUtility.MonitorInfo, double>();
                foreach (var candidate in candidates)
                {
                    double score = CalculateDirectionalScore(monitor, candidate, direction);
                    scores[candidate] = score;
                }

                // Retourner le candidat avec le meilleur score
                return scores.OrderByDescending(s => s.Value).First().Key;
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                Debug.WriteLine($"Erreur lors de la recherche d'un moniteur adjacent: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calcule un score pour évaluer la pertinence d'un moniteur adjacent
        /// </summary>
        /// <param name="source">Moniteur source</param>
        /// <param name="candidate">Moniteur candidat</param>
        /// <param name="direction">Direction désirée</param>
        /// <returns>Score (plus élevé = meilleur)</returns>
        private static double CalculateDirectionalScore(
            ScreenUtility.MonitorInfo source,
            ScreenUtility.MonitorInfo candidate,
            Direction direction)
        {
            try
            {
                // Vérifier que les moniteurs ne sont pas null
                if (source == null || candidate == null)
                    return 0.0;

                // Distance entre les centres des moniteurs
                Point sourceCenter = new Point(
                    source.Bounds.Left + source.Width / 2,
                    source.Bounds.Top + source.Height / 2);

                Point candidateCenter = new Point(
                    candidate.Bounds.Left + candidate.Width / 2,
                    candidate.Bounds.Top + candidate.Height / 2);

                // Calculer la distance dans la direction orthogonale à la direction désirée
                double orthogonalDistance;
                if (direction == Direction.Left || direction == Direction.Right)
                {
                    orthogonalDistance = Math.Abs(sourceCenter.Y - candidateCenter.Y);
                }
                else
                {
                    orthogonalDistance = Math.Abs(sourceCenter.X - candidateCenter.X);
                }

                // Plus la distance orthogonale est faible, meilleur est le score
                double score = 1000.0 / (orthogonalDistance + 1.0);

                // Vérifier si les moniteurs se chevauchent dans la direction orthogonale
                bool overlap = false;
                if (direction == Direction.Left || direction == Direction.Right)
                {
                    overlap = (source.Bounds.Top < candidate.Bounds.Bottom) &&
                              (source.Bounds.Bottom > candidate.Bounds.Top);
                }
                else
                {
                    overlap = (source.Bounds.Left < candidate.Bounds.Right) &&
                              (source.Bounds.Right > candidate.Bounds.Left);
                }

                // Si les moniteurs se chevauchent, leur donner un score plus élevé
                if (overlap)
                {
                    score *= 2.0;
                }

                // Distance dans la direction désirée
                double directionalDistance;
                switch (direction)
                {
                    case Direction.Left:
                        directionalDistance = source.Bounds.Left - candidate.Bounds.Right;
                        break;

                    case Direction.Right:
                        directionalDistance = candidate.Bounds.Left - source.Bounds.Right;
                        break;

                    case Direction.Up:
                        directionalDistance = source.Bounds.Top - candidate.Bounds.Bottom;
                        break;

                    case Direction.Down:
                        directionalDistance = candidate.Bounds.Top - source.Bounds.Bottom;
                        break;

                    default:
                        directionalDistance = double.MaxValue;
                        break;
                }

                // Plus la distance directionnelle est faible, meilleur est le score
                score *= 1000.0 / (directionalDistance + 1.0);

                return score;
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                Debug.WriteLine($"Erreur lors du calcul du score directionnel: {ex.Message}");
                return 0.0;
            }
        }

        /// <summary>
        /// Anime le déplacement d'une fenêtre entre deux positions
        /// </summary>
        /// <param name="window">Fenêtre à animer</param>
        /// <param name="targetLeft">Position X cible</param>
        /// <param name="targetTop">Position Y cible</param>
        /// <param name="duration">Durée de l'animation en millisecondes</param>
        /// <returns>Tâche représentant l'animation</returns>
        public static async Task AnimateWindowMovement(Window window, double targetLeft, double targetTop, int duration = 300)
        {
            // Vérifier que la fenêtre n'est pas null
            if (window == null)
                return;

            try
            {
                // Positions initiales
                double startLeft = window.Left;
                double startTop = window.Top;

                // Distances à parcourir
                double deltaLeft = targetLeft - startLeft;
                double deltaTop = targetTop - startTop;

                // Démarrer le minuteur
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                // Boucle d'animation
                while (stopwatch.ElapsedMilliseconds < duration)
                {
                    // Calculer la progression (0.0 à 1.0)
                    double progress = stopwatch.ElapsedMilliseconds / (double)duration;

                    // Appliquer une fonction d'atténuation pour un mouvement plus naturel
                    double easedProgress = EaseInOutCubic(progress);

                    // Calculer les nouvelles positions
                    double newLeft = startLeft + (deltaLeft * easedProgress);
                    double newTop = startTop + (deltaTop * easedProgress);

                    // Appliquer les nouvelles positions
                    window.Left = newLeft;
                    window.Top = newTop;

                    // Attendre la prochaine frame
                    await Task.Delay(16); // ~60 fps
                }

                // S'assurer que la position finale est exactement celle demandée
                window.Left = targetLeft;
                window.Top = targetTop;
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                Debug.WriteLine($"Erreur lors de l'animation de la fenêtre: {ex.Message}");

                // En cas d'erreur, positionner directement la fenêtre
                window.Left = targetLeft;
                window.Top = targetTop;
            }
        }

        /// <summary>
        /// Fonction d'atténuation cubique pour un mouvement plus naturel
        /// </summary>
        /// <param name="t">Progression (0.0 à 1.0)</param>
        /// <returns>Valeur atténuée</returns>
        private static double EaseInOutCubic(double t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
        }

        #endregion

        #region Calcul de positions optimales

        /// <summary>
        /// Calcule la position optimale pour une fenêtre de prévisualisation
        /// en tenant compte de l'espace disponible sur les écrans.
        /// Cette méthode est particulièrement importante pour le positionnement
        /// des fenêtres de prévisualisation par rapport à une fenêtre principale.
        /// </summary>
        /// <param name="targetWindowRect">Rectangle de la fenêtre cible</param>
        /// <param name="previewSize">Taille de la fenêtre de prévisualisation</param>
        /// <param name="screens">Liste des écrans disponibles (optionnel)</param>
        /// <returns>Point indiquant la position optimale</returns>
        public static Point CalculateOptimalPreviewPosition(
            Rect targetWindowRect,
            Size previewSize,
            IEnumerable<ScreenUtility.MonitorInfo> screens = null)
        {
            // Vérifier que les dimensions sont valides
            if (previewSize.Width <= 0 || previewSize.Height <= 0)
            {
                // Retourner une position par défaut (à droite de la fenêtre cible)
                return new Point(
                    targetWindowRect.Right + DEFAULT_MARGIN,
                    targetWindowRect.Top);
            }

            try
            {
                // Utiliser les écrans fournis ou récupérer tous les écrans disponibles
                var availableScreens = screens ?? ScreenUtility.Monitors;

                // S'assurer que la liste n'est pas vide ou null
                if (availableScreens == null || !availableScreens.Any())
                {
                    // Retourner une position par défaut (à droite de la fenêtre cible)
                    return new Point(
                        targetWindowRect.Right + DEFAULT_MARGIN,
                        targetWindowRect.Top);
                }

                // Utiliser le SnapZoneCalculator pour déterminer la position optimale
                SnapZoneCalculator calculator = new SnapZoneCalculator();
                return calculator.CalculateOptimalPosition(targetWindowRect, previewSize, availableScreens);
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                Debug.WriteLine($"Erreur lors du calcul de la position optimale: {ex.Message}");

                // En cas d'erreur, retourner une position par défaut (à droite de la fenêtre cible)
                return new Point(
                    targetWindowRect.Right + DEFAULT_MARGIN,
                    targetWindowRect.Top);
            }
        }

        #endregion

        #region Classes internes

        /// <summary>
        /// Classe utilitaire qui permet de calculer et gérer les zones d'accrochage
        /// pour le positionnement intelligent des fenêtres.
        /// </summary>
        public class SnapZoneCalculator
        {
            #region Constantes et membres privés

            // Distance (en pixels) à laquelle une fenêtre s'accroche à une autre
            private const int DEFAULT_SNAP_DISTANCE = 10;

            // Marges autour des écrans pour éviter de placer les fenêtres trop près des bords
            private const int SCREEN_MARGIN = 5;

            // Distance entre les fenêtres accrochées
            private int _snapDistance;

            // Options de zone d'accrochage
            private bool _snapToScreenEdges = true;
            private bool _snapToOtherWindows = true;

            // Cache des zones d'accrochage calculées
            private List<Rect> _cachedSnapZones = new List<Rect>();

            #endregion

            #region Constructeurs et initialisation

            /// <summary>
            /// Initialise une nouvelle instance de la classe SnapZoneCalculator avec les paramètres par défaut
            /// </summary>
            public SnapZoneCalculator()
                : this(DEFAULT_SNAP_DISTANCE)
            {
            }

            /// <summary>
            /// Initialise une nouvelle instance de la classe SnapZoneCalculator avec une distance d'accrochage personnalisée
            /// </summary>
            /// <param name="snapDistance">Distance (en pixels) à laquelle les fenêtres s'accrochent</param>
            public SnapZoneCalculator(int snapDistance)
            {
                _snapDistance = Math.Max(1, snapDistance);
            }

            #endregion

            #region Propriétés publiques

            /// <summary>
            /// Obtient ou définit la distance (en pixels) à laquelle les fenêtres s'accrochent
            /// </summary>
            public int SnapDistance
            {
                get { return _snapDistance; }
                set { _snapDistance = Math.Max(1, value); }
            }

            /// <summary>
            /// Obtient ou définit une valeur indiquant si les fenêtres doivent s'accrocher aux bords de l'écran
            /// </summary>
            public bool SnapToScreenEdges
            {
                get { return _snapToScreenEdges; }
                set { _snapToScreenEdges = value; }
            }

            /// <summary>
            /// Obtient ou définit une valeur indiquant si les fenêtres doivent s'accrocher à d'autres fenêtres
            /// </summary>
            public bool SnapToOtherWindows
            {
                get { return _snapToOtherWindows; }
                set { _snapToOtherWindows = value; }
            }

            #endregion

            #region Méthodes publiques

            /// <summary>
            /// Calcule la position optimale pour une fenêtre de prévisualisation
            /// en tenant compte de l'espace disponible sur les écrans.
            /// Cette méthode est critique pour résoudre le problème de positionnement
            /// près du bord droit de l'écran.
            /// </summary>
            /// <param name="targetWindowRect">Rectangle de la fenêtre cible</param>
            /// <param name="previewSize">Taille de la fenêtre de prévisualisation</param>
            /// <param name="screens">Liste des écrans disponibles</param>
            /// <returns>Point indiquant la position optimale</returns>
            public Point CalculateOptimalPosition(
                Rect targetWindowRect,
                Size previewSize,
                IEnumerable<ScreenUtility.MonitorInfo> screens)
            {
                try
                {
                    // Liste des positions potentielles à évaluer
                    List<Point> candidatePositions = new List<Point>();

                    // Déterminer l'écran qui contient la fenêtre cible
                    ScreenUtility.MonitorInfo targetScreen = null;
                    foreach (var screen in screens)
                    {
                        Rect screenRect = new Rect(
                            screen.Bounds.Left,
                            screen.Bounds.Top,
                            screen.Width,
                            screen.Height);

                        if (screenRect.IntersectsWith(targetWindowRect))
                        {
                            targetScreen = screen;
                            break;
                        }
                    }

                    // Si aucun écran ne contient la fenêtre, utiliser le premier écran
                    if (targetScreen == null && screens.Any())
                    {
                        targetScreen = screens.First();
                    }

                    // Si toujours aucun écran, retourner une position par défaut
                    if (targetScreen == null)
                    {
                        return new Point(
                            targetWindowRect.Right + _snapDistance,
                            targetWindowRect.Top);
                    }

                    // IMPORTANT: Vérifier si la fenêtre cible est proche du bord droit de l'écran
                    bool isNearRightEdge = IsNearRightEdge(targetWindowRect, targetScreen);

                    // Si la fenêtre est proche du bord droit, privilégier la position à gauche
                    if (isNearRightEdge)
                    {
                        // Ajouter en priorité les positions à gauche de la fenêtre cible
                        candidatePositions.Add(new Point(
                            targetWindowRect.Left - previewSize.Width - _snapDistance,
                            targetWindowRect.Top));

                        // Position à gauche alignée verticalement
                        candidatePositions.Add(new Point(
                            targetWindowRect.Left - previewSize.Width - _snapDistance,
                            targetWindowRect.Top + (targetWindowRect.Height - previewSize.Height) / 2));
                    }

                    // Ajouter les autres positions candidates
                    AddCandidatePositions(targetWindowRect, previewSize, candidatePositions, isNearRightEdge);

                    // Si aucune position n'a été trouvée, utiliser la position par défaut (à droite)
                    if (candidatePositions.Count == 0)
                    {
                        return new Point(
                            targetWindowRect.Right + _snapDistance,
                            targetWindowRect.Top);
                    }

                    // Évaluer chaque position candidate
                    Dictionary<Point, double> positionScores = new Dictionary<Point, double>();
                    foreach (Point position in candidatePositions)
                    {
                        positionScores[position] = EvaluatePosition(position, previewSize, targetWindowRect, screens);
                    }

                    // Retourner la position avec le meilleur score (plus élevé = meilleur)
                    var bestPosition = positionScores.OrderByDescending(p => p.Value).First().Key;

                    // Vérifier que la position est visible sur un écran
                    Rect previewRect = new Rect(bestPosition, previewSize);
                    if (!IsRectanglePartiallyVisible(previewRect, screens, 0.8))
                    {
                        // Si la position n'est pas suffisamment visible, contraindre aux limites de l'écran
                        previewRect = ConstrainRectToScreen(previewRect, screens);
                        bestPosition = previewRect.TopLeft;
                    }

                    return bestPosition;
                }
                catch (Exception ex)
                {
                    // Journaliser l'erreur
                    Debug.WriteLine($"Erreur dans CalculateOptimalPosition: {ex.Message}");

                    // En cas d'erreur, retourner une position par défaut (à droite)
                    return new Point(
                        targetWindowRect.Right + _snapDistance,
                        targetWindowRect.Top);
                }
            }

            /// <summary>
            /// Effacer le cache des zones d'accrochage
            /// </summary>
            public void ClearSnapZonesCache()
            {
                _cachedSnapZones.Clear();
            }

            /// <summary>
            /// Ajoute une zone d'accrochage personnalisée au cache
            /// </summary>
            /// <param name="zone">Zone d'accrochage à ajouter</param>
            public void AddCustomSnapZone(Rect zone)
            {
                if (!zone.IsEmpty)
                {
                    _cachedSnapZones.Add(zone);
                }
            }

            #endregion

            #region Méthodes privées

            /// <summary>
            /// Détermine si une fenêtre est proche du bord droit de l'écran
            /// </summary>
            /// <param name="windowRect">Rectangle de la fenêtre</param>
            /// <param name="screen">Écran à vérifier</param>
            /// <returns>True si la fenêtre est proche du bord droit, sinon False</returns>
            private bool IsNearRightEdge(Rect windowRect, ScreenUtility.MonitorInfo screen)
            {
                // Vérifier que les paramètres sont valides
                if (screen == null)
                    return false;

                // Créer un rectangle pour l'écran
                Rect screenRect = new Rect(
                    screen.Bounds.Left,
                    screen.Bounds.Top,
                    screen.Width,
                    screen.Height);

                // Définir un seuil adaptatif basé sur la largeur de l'écran (15% de la largeur)
                double threshold = screenRect.Width * 0.15;

                // IMPORTANT: Amplifier le seuil pour le bord droit pour compenser les problèmes de scaling
                if (GetDpiScaleFactor(screen) > 1.0)
                {
                    threshold *= 1.25; // Augmenter de 25% le seuil pour les écrans à haute résolution
                }

                // Calculer la distance entre le bord droit de la fenêtre et le bord droit de l'écran
                double distanceToRightEdge = screenRect.Right - windowRect.Right;

                // Vérifier si la distance est inférieure au seuil
                return distanceToRightEdge < threshold;
            }

            /// <summary>
            /// Vérifie si un rectangle est partiellement visible sur un des écrans fournis
            /// </summary>
            /// <param name="rect">Rectangle à vérifier</param>
            /// <param name="screens">Liste des écrans à vérifier</param>
            /// <param name="minVisiblePercent">Pourcentage minimal de visibilité</param>
            /// <returns>True si le rectangle est suffisamment visible, sinon False</returns>
            private bool IsRectanglePartiallyVisible(Rect rect, IEnumerable<ScreenUtility.MonitorInfo> screens, double minVisiblePercent)
            {
                // Vérifier que les paramètres sont valides
                if (screens == null || !screens.Any())
                    return true; // Par défaut, considérer visible si aucun écran n'est disponible

                double rectArea = rect.Width * rect.Height;
                if (rectArea <= 0)
                    return false; // Rectangle invalide

                foreach (var screen in screens)
                {
                    // Créer un rectangle pour l'écran
                    Rect screenRect = new Rect(
                        screen.Bounds.Left,
                        screen.Bounds.Top,
                        screen.Width,
                        screen.Height);

                    // Si le rectangle est entièrement contenu dans l'écran, retourner vrai
                    if (screenRect.Contains(rect))
                        return true;

                    // Vérifier l'intersection
                    if (rect.IntersectsWith(screenRect))
                    {
                        // Calculer la zone d'intersection
                        Rect intersection = Rect.Intersect(rect, screenRect);
                        double visibleArea = intersection.Width * intersection.Height;

                        // Vérifier si la partie visible dépasse le seuil minimal
                        if (visibleArea >= rectArea * minVisiblePercent)
                            return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Contraint un rectangle aux limites des écrans fournis
            /// </summary>
            /// <param name="rect">Rectangle à contraindre</param>
            /// <param name="screens">Liste des écrans à utiliser</param>
            /// <returns>Rectangle contraint</returns>
            private Rect ConstrainRectToScreen(Rect rect, IEnumerable<ScreenUtility.MonitorInfo> screens)
            {
                // Trouver le moniteur qui contient la plus grande partie du rectangle
                ScreenUtility.MonitorInfo bestMonitor = null;
                double bestOverlap = 0;

                foreach (var monitor in screens)
                {
                    // Créer un rectangle pour l'écran
                    Rect screenRect = new Rect(
                        monitor.Bounds.Left,
                        monitor.Bounds.Top,
                        monitor.Width,
                        monitor.Height);

                    // Calculer l'intersection
                    if (rect.IntersectsWith(screenRect))
                    {
                        Rect intersection = Rect.Intersect(rect, screenRect);
                        double overlap = intersection.Width * intersection.Height;

                        if (overlap > bestOverlap)
                        {
                            bestOverlap = overlap;
                            bestMonitor = monitor;
                        }
                    }
                }

                // Si aucun moniteur ne contient le rectangle, utiliser le plus proche
                if (bestMonitor == null)
                {
                    // Trouver le moniteur le plus proche du centre du rectangle
                    Point rectCenter = new Point(
                        rect.Left + rect.Width / 2,
                        rect.Top + rect.Height / 2);

                    double bestDistance = double.MaxValue;

                    foreach (var monitor in screens)
                    {
                        // Créer un rectangle pour l'écran
                        Rect screenRect = new Rect(
                            monitor.Bounds.Left,
                            monitor.Bounds.Top,
                            monitor.Width,
                            monitor.Height);

                        // Calculer le centre de l'écran
                        Point screenCenter = new Point(
                            screenRect.Left + screenRect.Width / 2,
                            screenRect.Top + screenRect.Height / 2);

                        // Calculer la distance entre les centres
                        double distance = Math.Sqrt(
                            Math.Pow(rectCenter.X - screenCenter.X, 2) +
                            Math.Pow(rectCenter.Y - screenCenter.Y, 2));

                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestMonitor = monitor;
                        }
                    }

                    // Si toujours pas de moniteur, utiliser le premier disponible
                    if (bestMonitor == null && screens.Any())
                    {
                        bestMonitor = screens.First();
                    }

                    // Si toujours pas de moniteur, retourner le rectangle inchangé
                    if (bestMonitor == null)
                        return rect;
                }

                // Créer un rectangle pour l'écran
                Rect monitorRect = new Rect(
                    bestMonitor.Bounds.Left,
                    bestMonitor.Bounds.Top,
                    bestMonitor.Width,
                    bestMonitor.Height);

                // Ajuster la taille si nécessaire
                double width = rect.Width;
                double height = rect.Height;

                if (width > monitorRect.Width)
                    width = monitorRect.Width;

                if (height > monitorRect.Height)
                    height = monitorRect.Height;

                // Ajuster la position
                double left = rect.Left;
                double top = rect.Top;

                // Ajustement horizontal
                if (left < monitorRect.Left)
                {
                    left = monitorRect.Left;
                }
                else if (left + width > monitorRect.Right)
                {
                    left = monitorRect.Right - width;
                }

                // Ajustement vertical
                if (top < monitorRect.Top)
                {
                    top = monitorRect.Top;
                }
                else if (top + height > monitorRect.Bottom)
                {
                    top = monitorRect.Bottom - height;
                }

                return new Rect(left, top, width, height);
            }

            /// <summary>
            /// Ajoute des positions candidates pour la fenêtre de prévisualisation
            /// </summary>
            /// <param name="targetRect">Rectangle de la fenêtre cible</param>
            /// <param name="previewSize">Taille de la fenêtre de prévisualisation</param>
            /// <param name="candidates">Liste des positions candidates à remplir</param>
            /// <param name="isNearRightEdge">Indique si la fenêtre cible est proche du bord droit</param>
            private void AddCandidatePositions(
                Rect targetRect,
                Size previewSize,
                List<Point> candidates,
                bool isNearRightEdge)
            {
                // Calculer le centre de la fenêtre cible
                double centerX = targetRect.Left + targetRect.Width / 2;
                double centerY = targetRect.Top + targetRect.Height / 2;

                if (!isNearRightEdge)
                {
                    // À droite de la fenêtre cible (position préférée si pas proche du bord droit)
                    candidates.Add(new Point(
                        targetRect.Right + _snapDistance,
                        targetRect.Top));

                    // À droite, aligné verticalement sur le centre
                    candidates.Add(new Point(
                        targetRect.Right + _snapDistance,
                        centerY - previewSize.Height / 2));
                }

                // En dessous de la fenêtre cible, aligné à gauche
                candidates.Add(new Point(
                    targetRect.Left,
                    targetRect.Bottom + _snapDistance));

                // En dessous, aligné horizontalement sur le centre
                candidates.Add(new Point(
                    centerX - previewSize.Width / 2,
                    targetRect.Bottom + _snapDistance));

                // À gauche de la fenêtre cible (déjà ajouté en priorité si près du bord droit)
                if (!isNearRightEdge)
                {
                    candidates.Add(new Point(
                        targetRect.Left - previewSize.Width - _snapDistance,
                        targetRect.Top));
                }

                // Au-dessus de la fenêtre cible, aligné à gauche
                candidates.Add(new Point(
                    targetRect.Left,
                    targetRect.Top - previewSize.Height - _snapDistance));

                // Au-dessus, aligné horizontalement sur le centre
                candidates.Add(new Point(
                    centerX - previewSize.Width / 2,
                    targetRect.Top - previewSize.Height - _snapDistance));

                // En dessous de la fenêtre cible, aligné à droite
                candidates.Add(new Point(
                    targetRect.Right - previewSize.Width,
                    targetRect.Bottom + _snapDistance));

                // À droite de la fenêtre cible, aligné en bas
                if (!isNearRightEdge)
                {
                    candidates.Add(new Point(
                        targetRect.Right + _snapDistance,
                        targetRect.Bottom - previewSize.Height));
                }

                // À gauche de la fenêtre cible, aligné en bas
                candidates.Add(new Point(
                    targetRect.Left - previewSize.Width - _snapDistance,
                    targetRect.Bottom - previewSize.Height));

                // Au-dessus de la fenêtre cible, aligné à droite
                candidates.Add(new Point(
                    targetRect.Right - previewSize.Width,
                    targetRect.Top - previewSize.Height - _snapDistance));

                // Positions en diagonale
                if (!isNearRightEdge)
                {
                    // En diagonale en bas à droite
                    candidates.Add(new Point(
                        targetRect.Right + _snapDistance,
                        targetRect.Bottom + _snapDistance));

                    // En diagonale en haut à droite
                    candidates.Add(new Point(
                        targetRect.Right + _snapDistance,
                        targetRect.Top - previewSize.Height - _snapDistance));
                }

                // En diagonale en bas à gauche
                candidates.Add(new Point(
                    targetRect.Left - previewSize.Width - _snapDistance,
                    targetRect.Bottom + _snapDistance));

                // En diagonale en haut à gauche
                candidates.Add(new Point(
                    targetRect.Left - previewSize.Width - _snapDistance,
                    targetRect.Top - previewSize.Height - _snapDistance));
            }

            /// <summary>
            /// Évalue une position candidate et retourne un score (plus élevé = meilleur)
            /// </summary>
            /// <param name="position">Position à évaluer</param>
            /// <param name="size">Taille de la fenêtre</param>
            /// <param name="targetRect">Rectangle de la fenêtre cible</param>
            /// <param name="screens">Liste des écrans disponibles</param>
            /// <returns>Score de la position (0.0 à 1.0)</returns>
            private double EvaluatePosition(Point position, Size size, Rect targetRect, IEnumerable<ScreenUtility.MonitorInfo> screens)
            {
                try
                {
                    // Créer un rectangle pour la position évaluée
                    Rect evaluatedRect = new Rect(position, size);

                    // Vérifier si la position est entièrement visible sur un écran
                    bool isFullyVisible = false;
                    double visibilityScore = 0.0;

                    foreach (var monitor in screens)
                    {
                        // Créer un rectangle pour l'écran
                        Rect screenRect = new Rect(
                            monitor.Bounds.Left,
                            monitor.Bounds.Top,
                            monitor.Width,
                            monitor.Height);

                        // Vérifier si le rectangle est entièrement contenu dans cet écran
                        if (screenRect.Contains(evaluatedRect))
                        {
                            isFullyVisible = true;
                            visibilityScore = 1.0;
                            break;
                        }

                        // Calculer le pourcentage de visibilité
                        if (evaluatedRect.IntersectsWith(screenRect))
                        {
                            Rect intersection = Rect.Intersect(evaluatedRect, screenRect);
                            double evaluatedArea = evaluatedRect.Width * evaluatedRect.Height;
                            double visibleArea = intersection.Width * intersection.Height;

                            double visiblePercent = visibleArea / evaluatedArea;
                            visibilityScore = Math.Max(visibilityScore, visiblePercent);
                        }
                    }

                    // Si la position n'est pas visible du tout, lui donner un score très bas
                    if (visibilityScore < 0.5)
                    {
                        return 0.0;
                    }

                    // Calculer la distance par rapport à la fenêtre cible
                    double distanceScore = CalculateProximityScore(evaluatedRect, targetRect);

                    // Calculer le score d'alignement (position alignée avec la fenêtre cible)
                    double alignmentScore = CalculateAlignmentScore(evaluatedRect, targetRect);

                    // Calculer le score final comme une moyenne pondérée
                    // La visibilité est le facteur le plus important
                    return (visibilityScore * 0.6) + (distanceScore * 0.2) + (alignmentScore * 0.2);
                }
                catch (Exception ex)
                {
                    // Journal d'erreur
                    Debug.WriteLine($"Erreur dans EvaluatePosition: {ex.Message}");
                    return 0.0;
                }
            }

            /// <summary>
            /// Calcule un score de proximité entre deux rectangles (plus proche = meilleur score)
            /// </summary>
            /// <param name="rect1">Premier rectangle</param>
            /// <param name="rect2">Second rectangle</param>
            /// <returns>Score de proximité (0.0 à 1.0)</returns>
            private double CalculateProximityScore(Rect rect1, Rect rect2)
            {
                try
                {
                    // Calculer les centres des rectangles
                    Point center1 = new Point(
                        rect1.Left + rect1.Width / 2,
                        rect1.Top + rect1.Height / 2);

                    Point center2 = new Point(
                        rect2.Left + rect2.Width / 2,
                        rect2.Top + rect2.Height / 2);

                    // Calculer la distance entre les centres
                    double distance = Math.Sqrt(
                        Math.Pow(center1.X - center2.X, 2) +
                        Math.Pow(center1.Y - center2.Y, 2));

                    // Transformer la distance en score (distance plus courte = score plus élevé)
                    // Utiliser une fonction exponentielle décroissante
                    return Math.Exp(-distance / 500.0);
                }
                catch (Exception ex)
                {
                    // Journal d'erreur
                    Debug.WriteLine($"Erreur dans CalculateProximityScore: {ex.Message}");
                    return 0.0;
                }
            }

            /// <summary>
            /// Calcule un score d'alignement entre deux rectangles
            /// </summary>
            /// <param name="rect1">Premier rectangle</param>
            /// <param name="rect2">Second rectangle</param>
            /// <returns>Score d'alignement (0.0 à 1.0)</returns>
            private double CalculateAlignmentScore(Rect rect1, Rect rect2)
            {
                try
                {
                    double score = 0.0;

                    // Vérifier l'alignement horizontal
                    if (Math.Abs(rect1.Left - rect2.Left) < _snapDistance)
                    {
                        // Alignement à gauche
                        score += 0.25;
                    }
                    else if (Math.Abs(rect1.Right - rect2.Right) < _snapDistance)
                    {
                        // Alignement à droite
                        score += 0.25;
                    }
                    else if (Math.Abs(rect1.Left - rect2.Right) < _snapDistance ||
                            Math.Abs(rect1.Right - rect2.Left) < _snapDistance)
                    {
                        // Rectangles adjacents horizontalement
                        score += 0.20;
                    }

                    // Vérifier l'alignement vertical
                    if (Math.Abs(rect1.Top - rect2.Top) < _snapDistance)
                    {
                        // Alignement en haut
                        score += 0.25;
                    }
                    else if (Math.Abs(rect1.Bottom - rect2.Bottom) < _snapDistance)
                    {
                        // Alignement en bas
                        score += 0.25;
                    }
                    else if (Math.Abs(rect1.Top - rect2.Bottom) < _snapDistance ||
                            Math.Abs(rect1.Bottom - rect2.Top) < _snapDistance)
                    {
                        // Rectangles adjacents verticalement
                        score += 0.20;
                    }

                    return Math.Min(1.0, score);
                }
                catch (Exception ex)
                {
                    // Journal d'erreur
                    Debug.WriteLine($"Erreur dans CalculateAlignmentScore: {ex.Message}");
                    return 0.0;
                }
            }

            #endregion
        }

        #endregion
    }
}
