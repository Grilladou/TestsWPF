using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using HelloWorld;  // Pour accéder à ScreenUtility

namespace HelloWorld.Preview
{
    /// <summary>
    /// Implémentation par défaut de IPositionStrategy qui place la fenêtre de prévisualisation
    /// adjacente à la fenêtre cible (à droite ou en dessous selon l'espace disponible).
    /// </summary>
    public class AdjacentPositionStrategy : IPositionStrategy
    {
        // Distance d'accrochage en pixels
        protected const int SnapDistance = 10;

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
            // Position par défaut (à droite de la fenêtre originale)
            double left = originalWindowPosition.X + originalWindowSize.Width + SnapDistance;
            double top = originalWindowPosition.Y;

            // Créer un rectangle pour la fenêtre cible
            Rect originalWindowRect = new Rect(originalWindowPosition, originalWindowSize);

            // Créer un rectangle pour la position par défaut
            Rect defaultPositionRect = new Rect(left, top, previewWindowSize.Width, previewWindowSize.Height);

            // Vérifier si la position par défaut est visible sur l'écran
            bool isVisible = IsVisibleOnScreen(defaultPositionRect, screens);

            // Si la position par défaut n'est pas visible, essayer les positions alternatives
            if (!isVisible)
            {
                // Journaliser pour le débogage
                System.Diagnostics.Debug.WriteLine("SnapPositionStrategy: Position par défaut non visible, essai de positions alternatives");

                // Liste des positions à essayer dans l'ordre de priorité
                List<Rect> positionsToTry = new List<Rect>();

                // En dessous, aligné à gauche
                positionsToTry.Add(new Rect(
                    originalWindowPosition.X,
                    originalWindowPosition.Y + originalWindowSize.Height + SnapDistance,
                    previewWindowSize.Width,
                    previewWindowSize.Height));

                // À gauche, aligné en haut
                positionsToTry.Add(new Rect(
                    originalWindowPosition.X - previewWindowSize.Width - SnapDistance,
                    originalWindowPosition.Y,
                    previewWindowSize.Width,
                    previewWindowSize.Height));

                // Au-dessus, aligné à gauche
                positionsToTry.Add(new Rect(
                    originalWindowPosition.X,
                    originalWindowPosition.Y - previewWindowSize.Height - SnapDistance,
                    previewWindowSize.Width,
                    previewWindowSize.Height));

                // À droite, aligné en bas
                positionsToTry.Add(new Rect(
                    originalWindowPosition.X + originalWindowSize.Width + SnapDistance,
                    originalWindowPosition.Y + originalWindowSize.Height - previewWindowSize.Height,
                    previewWindowSize.Width,
                    previewWindowSize.Height));

                // En dessous, aligné à droite
                positionsToTry.Add(new Rect(
                    originalWindowPosition.X + originalWindowSize.Width - previewWindowSize.Width,
                    originalWindowPosition.Y + originalWindowSize.Height + SnapDistance,
                    previewWindowSize.Width,
                    previewWindowSize.Height));

                // À gauche, aligné en bas
                positionsToTry.Add(new Rect(
                    originalWindowPosition.X - previewWindowSize.Width - SnapDistance,
                    originalWindowPosition.Y + originalWindowSize.Height - previewWindowSize.Height,
                    previewWindowSize.Width,
                    previewWindowSize.Height));

                // Au-dessus, aligné à droite
                positionsToTry.Add(new Rect(
                    originalWindowPosition.X + originalWindowSize.Width - previewWindowSize.Width,
                    originalWindowPosition.Y - previewWindowSize.Height - SnapDistance,
                    previewWindowSize.Width,
                    previewWindowSize.Height));

                // Essayer chaque position jusqu'à en trouver une visible
                foreach (Rect position in positionsToTry)
                {
                    if (IsVisibleOnScreen(position, screens))
                    {
                        // Position visible trouvée, l'utiliser
                        left = position.Left;
                        top = position.Top;
                        isVisible = true;
                        System.Diagnostics.Debug.WriteLine($"SnapPositionStrategy: Position alternative trouvée à X={left}, Y={top}");
                        break;
                    }
                }

                // Si aucune position n'est visible, utiliser la stratégie du centre de l'écran
                if (!isVisible)
                {
                    System.Diagnostics.Debug.WriteLine("SnapPositionStrategy: Aucune position d'accrochage n'est visible, centrage sur l'écran");

                    // Trouver l'écran qui contient la fenêtre originale
                    HelloWorld.ScreenUtility.MonitorInfo targetScreen = FindScreenContainingWindow(
                        originalWindowPosition, originalWindowSize, screens);

                    if (targetScreen != null)
                    {
                        // Centrer la fenêtre de prévisualisation sur cet écran
                        left = targetScreen.Bounds.Left + (targetScreen.Width - previewWindowSize.Width) / 2;
                        top = targetScreen.Bounds.Top + (targetScreen.Height - previewWindowSize.Height) / 2;
                        System.Diagnostics.Debug.WriteLine($"SnapPositionStrategy: Centrage sur écran à X={left}, Y={top}");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"SnapPositionStrategy: Position par défaut visible à X={left}, Y={top}");
            }

            // Créer un rectangle pour la position finale
            Rect finalRect = new Rect(left, top, previewWindowSize.Width, previewWindowSize.Height);

            // Une dernière vérification pour s'assurer que la position est entièrement visible
            // Si ce n'est pas le cas, ajuster la position pour qu'elle soit visible
            if (!WindowPositioningHelper.IsRectangleFullyVisible(finalRect))
            {
                System.Diagnostics.Debug.WriteLine("SnapPositionStrategy: Ajustement final pour garantir la visibilité complète");

                // Utiliser l'utilitaire ScreenUtilityExtensions pour contraindre le rectangle aux limites de l'écran
                Rect constrainedRect = WindowPositioningHelper.ConstrainRectToScreen(finalRect);

                // Appliquer la position contrainte
                left = constrainedRect.Left;
                top = constrainedRect.Top;

                System.Diagnostics.Debug.WriteLine($"SnapPositionStrategy: Position ajustée à X={left}, Y={top}");
            }

            return new Point(left, top);
        }

        /// <summary>
        /// Vérifie si un rectangle est visible sur l'un des écrans disponibles
        /// </summary>
        /// <param name="rect">Rectangle à vérifier</param>
        /// <param name="screens">Liste des écrans disponibles</param>
        /// <returns>True si le rectangle est visible, sinon False</returns>
        private bool IsVisibleOnScreen(Rect rect, IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens)
        {
            // Utiliser directement la méthode dans WindowPositioningHelper
            return WindowPositioningHelper.IsRectanglePartiallyVisible(rect, 0.8);
        }

        /// <summary>
        /// Trouve l'écran qui contient la fenêtre principale
        /// </summary>
        /// <param name="windowPosition">Position de la fenêtre</param>
        /// <param name="windowSize">Taille de la fenêtre</param>
        /// <param name="screens">Liste des écrans disponibles</param>
        /// <returns>Information sur l'écran contenant la fenêtre, ou null si aucun écran ne contient la fenêtre</returns>
        private HelloWorld.ScreenUtility.MonitorInfo FindScreenContainingWindow(Point windowPosition, Size windowSize, IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens)
        {
            // Créer un rectangle représentant la fenêtre
            Rect windowRect = new Rect(windowPosition, windowSize);

            // Utiliser la méthode du WindowPositioningHelper
            return WindowPositioningHelper.FindBestMonitorForRect(windowRect);
        }
    }

    /// <summary>
    /// Stratégie de positionnement qui centre la fenêtre de prévisualisation sur l'écran 
    /// contenant la fenêtre originale.
    /// </summary>
    public class CenterScreenPositionStrategy : IPositionStrategy
    {
        /// <summary>
        /// Calcule la position où la fenêtre de prévisualisation devrait être placée
        /// </summary>
        /// <param name="originalWindowPosition">Position actuelle de la fenêtre principale</param>
        /// <param name="originalWindowSize">Taille actuelle de la fenêtre principale</param>
        /// <param name="previewWindowSize">Taille prévue pour la fenêtre de prévisualisation</param>
        /// <param name="screens">Informations sur les écrans disponibles</param>
        /// <returns>Point indiquant où positionner la fenêtre de prévisualisation</returns>
        public Point CalculatePosition(Point originalWindowPosition, Size originalWindowSize,
                                      Size previewWindowSize, IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens)
        {
            // Position par défaut (centrée sur l'écran principal)
            double left = (SystemParameters.PrimaryScreenWidth - previewWindowSize.Width) / 2;
            double top = (SystemParameters.PrimaryScreenHeight - previewWindowSize.Height) / 2;

            // Si nous avons des informations sur les écrans, essayer de trouver l'écran qui contient la fenêtre originale
            if (screens != null && screens.Any())
            {
                // Créer un rectangle représentant la fenêtre originale
                Rect windowRect = new Rect(originalWindowPosition, originalWindowSize);

                // Rechercher l'écran qui contient le centre de la fenêtre originale
                Point windowCenter = new Point(
                    originalWindowPosition.X + originalWindowSize.Width / 2,
                    originalWindowPosition.Y + originalWindowSize.Height / 2);

                foreach (var screen in screens)
                {
                    // Convertir la structure RECT de ScreenUtility en Rect de WPF
                    Rect screenRect = new Rect(
                        screen.Bounds.Left,
                        screen.Bounds.Top,
                        screen.Width,
                        screen.Height);

                    // Vérifier si l'écran contient le centre de la fenêtre
                    if (screenRect.Contains(windowCenter))
                    {
                        // Centrer la fenêtre de prévisualisation sur cet écran
                        left = screen.Bounds.Left + (screen.Width - previewWindowSize.Width) / 2;
                        top = screen.Bounds.Top + (screen.Height - previewWindowSize.Height) / 2;
                        break;
                    }
                }
            }

            return new Point(left, top);
        }
    }

    /// <summary>
    /// Stratégie de positionnement qui place la fenêtre de prévisualisation à côté de 
    /// la fenêtre originale en utilisant des zones d'accrochage intelligentes.
    /// </summary>
    public class SnapPositionStrategy : IPositionStrategy
    {
        // Distance d'accrochage en pixels
        protected const int SnapDistance = 10;

        /// <summary>
        /// Calcule la position où la fenêtre de prévisualisation devrait être placée
        /// </summary>
        /// <param name="originalWindowPosition">Position actuelle de la fenêtre principale</param>
        /// <param name="originalWindowSize">Taille actuelle de la fenêtre principale</param>
        /// <param name="previewWindowSize">Taille prévue pour la fenêtre de prévisualisation</param>
        /// <param name="screens">Informations sur les écrans disponibles</param>
        /// <returns>Point indiquant où positionner la fenêtre de prévisualisation</returns>
        public Point CalculatePosition(Point originalWindowPosition, Size originalWindowSize,
                                      Size previewWindowSize, IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens)
        {
            // Position par défaut (à droite de la fenêtre originale)
            double left = originalWindowPosition.X + originalWindowSize.Width + SnapDistance;
            double top = originalWindowPosition.Y;

            // Vérifier si cette position est visible sur l'écran
            if (!IsVisibleOnScreen(new Rect(left, top, previewWindowSize.Width, previewWindowSize.Height), screens))
            {
                // Essayer différentes positions d'accrochage dans l'ordre de priorité
                Point[] snapPositions = new Point[]
                {
                    // En dessous, aligné à gauche
                    new Point(originalWindowPosition.X, originalWindowPosition.Y + originalWindowSize.Height + SnapDistance),
                    
                    // À gauche, aligné en haut
                    new Point(originalWindowPosition.X - previewWindowSize.Width - SnapDistance, originalWindowPosition.Y),
                    
                    // Au-dessus, aligné à gauche
                    new Point(originalWindowPosition.X, originalWindowPosition.Y - previewWindowSize.Height - SnapDistance),
                    
                    // À droite, aligné en bas
                    new Point(originalWindowPosition.X + originalWindowSize.Width + SnapDistance,
                             originalWindowPosition.Y + originalWindowSize.Height - previewWindowSize.Height),
                    
                    // En dessous, aligné à droite
                    new Point(originalWindowPosition.X + originalWindowSize.Width - previewWindowSize.Width,
                             originalWindowPosition.Y + originalWindowSize.Height + SnapDistance),
                    
                    // À gauche, aligné en bas
                    new Point(originalWindowPosition.X - previewWindowSize.Width - SnapDistance,
                             originalWindowPosition.Y + originalWindowSize.Height - previewWindowSize.Height),
                    
                    // Au-dessus, aligné à droite
                    new Point(originalWindowPosition.X + originalWindowSize.Width - previewWindowSize.Width,
                             originalWindowPosition.Y - previewWindowSize.Height - SnapDistance)
                };

                // Trouver la première position visible
                foreach (var position in snapPositions)
                {
                    if (IsVisibleOnScreen(new Rect(position, previewWindowSize), screens))
                    {
                        left = position.X;
                        top = position.Y;
                        break;
                    }
                }

                // Si aucune position d'accrochage n'est visible, utiliser la stratégie du centre de l'écran
                if (!IsVisibleOnScreen(new Rect(left, top, previewWindowSize.Width, previewWindowSize.Height), screens))
                {
                    var centerStrategy = new CenterScreenPositionStrategy();
                    Point centerPosition = centerStrategy.CalculatePosition(originalWindowPosition, originalWindowSize, previewWindowSize, screens);
                    left = centerPosition.X;
                    top = centerPosition.Y;
                }
            }

            return new Point(left, top);
        }

        /// <summary>
        /// Vérifie si un rectangle est visible sur l'un des écrans disponibles
        /// </summary>
        /// <param name="rect">Rectangle à vérifier</param>
        /// <param name="screens">Liste des écrans disponibles</param>
        /// <returns>True si le rectangle est visible, sinon False</returns>
        private bool IsVisibleOnScreen(Rect rect, IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens)
        {
            if (screens == null || !screens.Any())
                return true; // Si aucune information d'écran n'est disponible, on suppose que c'est visible

            foreach (var screen in screens)
            {
                // Convertir les structures RECT de ScreenUtility en Rect de WPF
                Rect screenRect = new Rect(
                    screen.Bounds.Left,
                    screen.Bounds.Top,
                    screen.Width,
                    screen.Height);

                // Vérifier si le rectangle est entièrement visible sur cet écran
                if (screenRect.Contains(rect))
                {
                    return true;
                }

                // Ou si une partie significative (au moins 90%) du rectangle est visible
                if (rect.IntersectsWith(screenRect))
                {
                    Rect intersection = Rect.Intersect(rect, screenRect);
                    double visibleArea = intersection.Width * intersection.Height;
                    double totalArea = rect.Width * rect.Height;

                    if (visibleArea >= totalArea * 0.9)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}