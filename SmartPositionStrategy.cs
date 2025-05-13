using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Windows;
using HelloWorld;  // Pour accéder à ScreenUtility

namespace HelloWorld.Preview
{
    /// <summary>
    /// Stratégie de positionnement intelligente qui analyse la position de la fenêtre principale
    /// par rapport aux bords de l'écran et l'espace disponible pour déterminer le meilleur emplacement 
    /// pour la fenêtre de prévisualisation.
    /// 
    /*  Récapitulatif des modifications

            1. Ajout d'une nouvelle méthode IsHorizontalSpaceProportionallyBetter pour comparer l'espace disponible horizontalement et verticalement et déterminer quelle orientation privilégier.

            2. Refonte complète de GetCandidatePositions pour intégrer la nouvelle logique de priorisation basée sur l'espace proportionnel. La méthode prend désormais en compte si l'espace horizontal est plus favorable que l'espace vertical avant de décider quelle position privilégier.

            3. Amélioration de CalculateAvailableSpace pour mieux gérer le scaling et les conversions entre coordonnées logiques et physiques. La méthode utilise maintenant explicitement WindowPositioningHelper.GetDpiScaleFactor pour obtenir le facteur d'échelle DPI et applique ce facteur de manière cohérente lors de la conversion entre les deux systèmes de coordonnées. Cette approche garantit que les espaces disponibles calculés reflètent fidèlement les proportions réelles de l'écran, même avec des facteurs de scaling élevés.De plus, des facteurs de correction spécifiques sont appliqués lorsque la fenêtre est proche des bords de l'écran, ce qui est particulièrement important pour résoudre le problème de positionnement près du bord droit.
            Cette amélioration est fondamentale car elle assure que les calculs de positionnement fonctionnent correctement dans tous les environnements, quelle que soit la configuration d'affichage ou le scaling utilisé par l'utilisateur.

            4. Amélioration de GetBestVisiblePosition pour prendre en compte les préférences horizontales/verticales lors de l'évaluation des positions candidates, et appliquer des bonus de score appropriés.

            5. Renforcement de DetermineNearEdges pour une meilleure détection des bords d'écran, particulièrement du bord droit qui est crucial pour le problème identifié. La méthode tient désormais mieux compte du scaling.

            6. Mise à jour de CalculatePosition avec plus de vérifications, de journalisation et une meilleure gestion des erreurs.

        Ajustements fins possibles
            1. Le HORIZONTAL_PREFERENCE_FACTOR (dans IsHorizontalSpaceProportionallyBetter) peut être modifié pour renforcer ou réduire la préférence pour les positions horizontales
            2. Les facteurs de correction dans CalculateAvailableSpace (1.2, etc.) peuvent être ajustés pour mieux compenser les effets du scaling
            3. Les bonus de score dans GetBestVisiblePosition peuvent être modifiés pour privilégier davantage certaines positions
    */
    ///
    /// </summary>
    public class SmartPositionStrategy : IPositionStrategy
    {
        #region Constantes et types d'énumération

        // Pourcentage de l'écran considéré comme "proche" d'un bord
        // 15% de la dimension de l'écran est considéré comme "proche"
        private const double PROXIMITY_PERCENT = 0.15;

        // Distance minimale entre les fenêtres (marge)
        private const int SPACING = 10;

        // Pourcentage minimal de visibilité pour qu'une position soit considérée comme "visible"
        // On commence avec un critère strict (100%) puis on réduit progressivement si nécessaire
        private readonly double[] VISIBILITY_THRESHOLDS = { 1.0, 0.9, 0.8, 0.7, 0.5 };

        /// <summary>
        /// Énumération des bords d'écran
        /// </summary>
        [Flags]
        private enum ScreenEdge
        {
            None = 0,
            Left = 1,
            Right = 2,
            Top = 4,
            Bottom = 8
        }

        /// <summary>
        /// Positions relatives possibles de la fenêtre de prévisualisation
        /// </summary>
        private enum RelativePosition
        {
            Right,          // À droite, aligné en haut
            Left,           // À gauche, aligné en haut
            Bottom,         // En dessous, aligné à gauche
            Top,            // Au-dessus, aligné à gauche
            RightAligned,   // À droite, conservant l'alignement vertical (centré)
            LeftAligned,    // À gauche, conservant l'alignement vertical (centré)
            BottomAligned,  // En dessous, conservant l'alignement horizontal (centré)
            TopAligned,     // Au-dessus, conservant l'alignement horizontal (centré)
            BottomRight,    // Diagonale bas-droite
            BottomLeft,     // Diagonale bas-gauche
            TopRight,       // Diagonale haut-droite
            TopLeft         // Diagonale haut-gauche
        }

        /// <summary>
        /// Structure pour stocker l'espace disponible dans chaque direction
        /// </summary>
        private struct AvailableSpace
        {
            public double Left;
            public double Right;
            public double Top;
            public double Bottom;

            public override string ToString()
            {
                return $"Left: {Left}, Right: {Right}, Top: {Top}, Bottom: {Bottom}";
            }
        }

        #endregion

        #region Implémentation de IPositionStrategy

        /// <summary>
        /// Calcule la position optimale où la fenêtre de prévisualisation devrait être placée
        /// en fonction de la position de la fenêtre principale, des bords de l'écran
        /// et de l'espace disponible.
        /// </summary>
        /// <param name="originalWindowPosition">Position actuelle de la fenêtre principale</param>
        /// <param name="originalWindowSize">Taille actuelle de la fenêtre principale</param>
        /// <param name="previewWindowSize">Taille prévue pour la fenêtre de prévisualisation</param>
        /// <param name="screens">Informations sur les écrans disponibles</param>
        /// <returns>Point indiquant où positionner la fenêtre de prévisualisation</returns>
        public Point CalculatePosition(
            Point originalWindowPosition,
            Size originalWindowSize,
            Size previewWindowSize,
            IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens)
        {
            // Vérification des paramètres d'entrée
            if (screens == null || !screens.Any())
            {
                // Si aucune information d'écran n'est disponible, utiliser une position par défaut
                System.Diagnostics.Debug.WriteLine("Attention: Aucune information d'écran disponible");
                return new Point(
                    originalWindowPosition.X + originalWindowSize.Width + SPACING,
                    originalWindowPosition.Y);
            }

            // Vérification de la taille de la prévisualisation
            if (previewWindowSize.Width <= 0 || previewWindowSize.Height <= 0)
            {
                System.Diagnostics.Debug.WriteLine("Erreur: Dimensions de prévisualisation invalides");
                return new Point(
                    originalWindowPosition.X + originalWindowSize.Width + SPACING,
                    originalWindowPosition.Y);
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("==== Début du calcul de position ====");
                System.Diagnostics.Debug.WriteLine($"Fenêtre principale: Position=({originalWindowPosition.X},{originalWindowPosition.Y}), " +
                                                $"Taille={originalWindowSize.Width}x{originalWindowSize.Height}");
                System.Diagnostics.Debug.WriteLine($"Prévisualisation: Taille={previewWindowSize.Width}x{previewWindowSize.Height}");

                // 1. Créer un rectangle représentant la fenêtre principale
                Rect originalWindowRect = new Rect(originalWindowPosition, originalWindowSize);

                // 2. Trouver l'écran qui contient la fenêtre principale
                HelloWorld.ScreenUtility.MonitorInfo currentScreen = WindowPositioningHelper.FindBestMonitorForRect(originalWindowRect);

                // Si aucun écran ne contient la fenêtre principale, utiliser l'écran principal ou le premier écran disponible
                if (currentScreen == null)
                {
                    currentScreen = screens.FirstOrDefault(s => s.IsPrimary) ?? screens.First();
                    System.Diagnostics.Debug.WriteLine("Écran optimal non trouvé, utilisation de l'écran principal par défaut");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Écran optimal trouvé: {currentScreen.Index}, {currentScreen.Width}x{currentScreen.Height}");
                }

                // 3. Calculer l'espace disponible dans chaque direction
                AvailableSpace availableSpace = CalculateAvailableSpace(originalWindowRect, currentScreen);

                // 4. Déterminer les bords d'écran proches de la fenêtre principale
                ScreenEdge nearEdges = DetermineNearEdges(originalWindowRect, currentScreen, availableSpace);

                // Journaliser les informations pour le débogage
                System.Diagnostics.Debug.WriteLine($"Fenêtre: {originalWindowRect.Left},{originalWindowRect.Top} - {originalWindowRect.Width}x{originalWindowRect.Height}");
                System.Diagnostics.Debug.WriteLine($"Écran: {currentScreen.Bounds.Left},{currentScreen.Bounds.Top} - {currentScreen.Width}x{currentScreen.Height}");
                System.Diagnostics.Debug.WriteLine($"Espace disponible: {availableSpace}");
                System.Diagnostics.Debug.WriteLine($"Bords proches: {nearEdges}");

                // 5. Obtenir la liste des positions candidates par ordre de priorité
                List<RelativePosition> positionCandidates = GetCandidatePositions(nearEdges, availableSpace, previewWindowSize);

                // 6. Tester chaque position candidate avec des seuils de visibilité décroissants
                Point bestPosition = GetBestVisiblePosition(
                    originalWindowPosition,
                    originalWindowSize,
                    previewWindowSize,
                    positionCandidates,
                    screens);

                // 7. Contraindre la position finale aux limites de l'écran
                Rect finalRect = new Rect(bestPosition, previewWindowSize);
                Rect constrainedRect = WindowPositioningHelper.ConstrainRectToScreen(finalRect);

                System.Diagnostics.Debug.WriteLine($"Position finale: ({constrainedRect.Left},{constrainedRect.Top})");
                System.Diagnostics.Debug.WriteLine("==== Fin du calcul de position ====");

                return constrainedRect.TopLeft;
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur dans SmartPositionStrategy.CalculatePosition: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);

                // En cas d'erreur, retourner une position par défaut (à droite)
                return new Point(
                    originalWindowPosition.X + originalWindowSize.Width + SPACING,
                    originalWindowPosition.Y);
            }
        }

        #endregion

        #region Méthodes de calcul d'espace disponible

        /// <summary>
        /// Calcule l'espace disponible dans chaque direction autour de la fenêtre principale
        /// Version améliorée avec une meilleure gestion du scaling et des facteurs de correction
        /// </summary>
        /// <param name="windowRect">Rectangle représentant la fenêtre principale</param>
        /// <param name="screen">Écran contenant la fenêtre principale</param>
        /// <returns>Structure contenant l'espace disponible dans chaque direction</returns>
        private AvailableSpace CalculateAvailableSpace(Rect windowRect, HelloWorld.ScreenUtility.MonitorInfo screen)
        {
            // Vérification des paramètres d'entrée
            if (screen == null)
            {
                // Retourner des valeurs nulles (aucun espace disponible)
                return new AvailableSpace();
            }

            // Récupérer le facteur d'échelle DPI
            double dpiScaleFactor = WindowPositioningHelper.GetDpiScaleFactor(screen);
            System.Diagnostics.Debug.WriteLine($"Facteur d'échelle DPI: {dpiScaleFactor}");

            // Créer une version du rectangle de la fenêtre en coordonnées physiques
            Rect physicalWindowRect = new Rect(
                windowRect.X * dpiScaleFactor,
                windowRect.Y * dpiScaleFactor,
                windowRect.Width * dpiScaleFactor,
                windowRect.Height * dpiScaleFactor
            );

            // Calculer l'espace disponible en coordonnées physiques 
            AvailableSpace physicalSpace = new AvailableSpace
            {
                // À gauche de la fenêtre
                Left = physicalWindowRect.Left - screen.Bounds.Left,

                // À droite de la fenêtre
                Right = screen.Bounds.Right - (physicalWindowRect.Left + physicalWindowRect.Width),

                // Au-dessus de la fenêtre
                Top = physicalWindowRect.Top - screen.Bounds.Top,

                // En dessous de la fenêtre
                Bottom = screen.Bounds.Bottom - (physicalWindowRect.Top + physicalWindowRect.Height)
            };

            // Convertir les espaces en coordonnées logiques pour les calculs ultérieurs
            AvailableSpace logicalSpace = new AvailableSpace
            {
                Left = physicalSpace.Left / dpiScaleFactor,
                Right = physicalSpace.Right / dpiScaleFactor,
                Top = physicalSpace.Top / dpiScaleFactor,
                Bottom = physicalSpace.Bottom / dpiScaleFactor
            };

            // Appliquer des facteurs de correction pour compenser les problèmes de scaling
            // Correction spéciale pour le bord droit - particulièrement important
            if (screen.Bounds.Right - (physicalWindowRect.Left + physicalWindowRect.Width) < screen.Width * 0.20)
            {
                // La fenêtre est à moins de 20% de la largeur de l'écran du bord droit
                // Cette correction est critique pour résoudre le problème initial
                logicalSpace.Left *= 1.2; // Augmenter de 20% l'espace perçu disponible à gauche
                System.Diagnostics.Debug.WriteLine($"Correction d'espace à gauche appliquée: {logicalSpace.Left}");
            }

            // Correction similaire pour le bord gauche
            if (physicalWindowRect.Left - screen.Bounds.Left < screen.Width * 0.20)
            {
                logicalSpace.Right *= 1.2; // Augmenter de 20% l'espace perçu disponible à droite
                System.Diagnostics.Debug.WriteLine($"Correction d'espace à droite appliquée: {logicalSpace.Right}");
            }

            // Journaliser les valeurs calculées pour faciliter le débogage
            System.Diagnostics.Debug.WriteLine($"Espace physique calculé: Left: {physicalSpace.Left}, Right: {physicalSpace.Right}, Top: {physicalSpace.Top}, Bottom: {physicalSpace.Bottom}");
            System.Diagnostics.Debug.WriteLine($"Espace logique après correction: {logicalSpace}");

            return logicalSpace;
        }

        /// <summary>
        /// Détermine quels bords de l'écran sont proches de la fenêtre principale
        /// en utilisant un seuil adaptatif basé sur la taille de l'écran.
        /// Version améliorée avec une détection plus précise du bord droit.
        /// </summary>
        /// <param name="windowRect">Rectangle représentant la fenêtre principale</param>
        /// <param name="screen">Écran contenant la fenêtre principale</param>
        /// <param name="space">Structure contenant l'espace disponible dans chaque direction</param>
        /// <returns>Combinaison des bords d'écran proches</returns>
        private ScreenEdge DetermineNearEdges(
            Rect windowRect,
            HelloWorld.ScreenUtility.MonitorInfo screen,
            AvailableSpace space)
        {
            // Vérification des paramètres d'entrée
            if (screen == null)
                return ScreenEdge.None;

            try
            {
                // Récupérer le facteur d'échelle DPI
                double dpiScaleFactor = WindowPositioningHelper.GetDpiScaleFactor(screen);

                // Calculer les seuils adaptatifs pour chaque dimension
                // en tenant compte du facteur d'échelle
                double horizontalThreshold = screen.Width * PROXIMITY_PERCENT;
                double verticalThreshold = screen.Height * PROXIMITY_PERCENT;

                // Adapter les seuils en fonction du scaling
                if (dpiScaleFactor > 1.0)
                {
                    // Rendre les seuils plus sensibles avec scaling élevé
                    double scalingFactor = 1.0 + ((dpiScaleFactor - 1.0) * 0.5);
                    horizontalThreshold *= scalingFactor;
                    verticalThreshold *= scalingFactor;

                    System.Diagnostics.Debug.WriteLine($"Seuils adaptés au scaling: H={horizontalThreshold}, V={verticalThreshold}");
                }

                // Déterminer les bords proches en fonction des marges
                ScreenEdge edges = ScreenEdge.None;

                // Adaptation spéciale pour le bord droit - plus sensible
                // Cela garantit que nous détectons plus tôt la proximité du bord droit
                double rightThreshold = horizontalThreshold * 1.25; // 25% plus sensible
                if (windowRect.Right > screen.Bounds.Right - rightThreshold)
                {
                    edges |= ScreenEdge.Right;
                    System.Diagnostics.Debug.WriteLine("Détection améliorée du bord droit activée");
                }

                // Vérifications standard
                if (space.Left < horizontalThreshold)
                {
                    edges |= ScreenEdge.Left;
                    System.Diagnostics.Debug.WriteLine("Bord gauche détecté");
                }

                // La vérification du bord droit est déjà faite avec un seuil augmenté

                if (space.Top < verticalThreshold)
                {
                    edges |= ScreenEdge.Top;
                    System.Diagnostics.Debug.WriteLine("Bord supérieur détecté");
                }

                if (space.Bottom < verticalThreshold)
                {
                    edges |= ScreenEdge.Bottom;
                    System.Diagnostics.Debug.WriteLine("Bord inférieur détecté");
                }

                return edges;
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur dans DetermineNearEdges: {ex.Message}");

                // Retourner ScreenEdge.None en cas d'erreur
                return ScreenEdge.None;
            }
        }

        /// <summary>
        /// Compare les espaces proportionnels disponibles horizontalement et verticalement
        /// pour déterminer quelle direction privilégier pour le positionnement
        /// </summary>
        /// <param name="space">Structure contenant l'espace disponible dans chaque direction</param>
        /// <param name="previewSize">Taille de la fenêtre de prévisualisation</param>
        /// <returns>True si l'espace horizontal est proportionnellement plus favorable</returns>
        private bool IsHorizontalSpaceProportionallyBetter(AvailableSpace space, Size previewSize)
        {
            // Vérification des valeurs nulles ou négatives
            if (previewSize.Width <= 0 || previewSize.Height <= 0)
            {
                return true; // Par défaut, privilégier horizontal en cas de valeurs invalides
            }

            // Calculer l'espace total disponible dans chaque direction
            double totalHorizontalSpace = space.Left + space.Right;
            double totalVerticalSpace = space.Top + space.Bottom;

            // Éviter la division par zéro
            if (totalVerticalSpace <= 0)
            {
                return true;
            }
            if (totalHorizontalSpace <= 0)
            {
                return false;
            }

            // Calculer les rapports d'espace nécessaire vs disponible
            double horizontalSpaceRatio = Math.Max(space.Left, space.Right) / previewSize.Width;
            double verticalSpaceRatio = Math.Max(space.Top, space.Bottom) / previewSize.Height;

            // Facteur de préférence pour l'orientation horizontale (ajustable selon les préférences)
            const double HORIZONTAL_PREFERENCE_FACTOR = 1.2;

            // Comparer les rapports, avec une légère préférence pour l'horizontal
            return horizontalSpaceRatio * HORIZONTAL_PREFERENCE_FACTOR >= verticalSpaceRatio;
        }

        #endregion

        #region Méthodes de génération et évaluation des positions candidates

        /// <summary>
        /// Obtient la liste des positions candidates par ordre de priorité en fonction
        /// des bords proches et de l'espace disponible.
        /// Version améliorée avec une priorisation basée sur les proportions d'espace disponible.
        /// </summary>
        /// <param name="nearEdges">Bords proches de la fenêtre principale</param>
        /// <param name="space">Espace disponible dans chaque direction</param>
        /// <param name="previewSize">Taille de la fenêtre de prévisualisation</param>
        /// <returns>Liste des positions candidates par ordre de priorité</returns>
        private List<RelativePosition> GetCandidatePositions(
            ScreenEdge nearEdges,
            AvailableSpace space,
            Size previewSize)
        {
            var candidates = new List<RelativePosition>();

            // Évaluer si la prévisualisation peut tenir dans chaque direction
            bool fitsRight = space.Right >= previewSize.Width + SPACING;
            bool fitsLeft = space.Left >= previewSize.Width + SPACING;
            bool fitsBottom = space.Bottom >= previewSize.Height + SPACING;
            bool fitsTop = space.Top >= previewSize.Height + SPACING;

            System.Diagnostics.Debug.WriteLine($"Fits: Right={fitsRight}, Left={fitsLeft}, Bottom={fitsBottom}, Top={fitsTop}");

            // Déterminer si l'espace horizontal est proportionnellement meilleur que vertical
            bool preferHorizontal = IsHorizontalSpaceProportionallyBetter(space, previewSize);
            System.Diagnostics.Debug.WriteLine($"Préférence pour positionnement horizontal: {preferHorizontal}");

            // RÈGLE PRIORITAIRE: Si la fenêtre est proche du bord droit,
            // FORCER la position à gauche avec une priorité absolue
            if ((nearEdges & ScreenEdge.Right) != 0)
            {
                // Vider la liste pour ignorer toute position précédente
                candidates.Clear();

                // Priorité absolue aux positions à gauche
                candidates.Add(RelativePosition.LeftAligned);  // Gauche alignée (même position verticale) - PRIORITÉ ABSOLUE
                candidates.Add(RelativePosition.Left);         // Gauche en haut

                // Si l'espace horizontal n'est vraiment pas favorable, considérer les positions verticales
                if (!preferHorizontal)
                {
                    if (fitsBottom)
                    {
                        candidates.Add(RelativePosition.BottomAligned); // Dessous alignée
                        candidates.Add(RelativePosition.Bottom);        // Dessous à gauche
                    }

                    if (fitsTop)
                    {
                        candidates.Add(RelativePosition.TopAligned);   // Dessus alignée
                        candidates.Add(RelativePosition.Top);          // Dessus à gauche
                    }
                }

                // Coins - toujours préférer ceux avec la position à gauche
                if (fitsBottom) candidates.Add(RelativePosition.BottomLeft);
                if (fitsTop) candidates.Add(RelativePosition.TopLeft);

                // En tout dernier recours (vraiment pas recommandé, car probablement masqué)
                if (fitsRight)
                {
                    candidates.Add(RelativePosition.RightAligned);
                    candidates.Add(RelativePosition.Right);
                }

                // Journaliser cette situation spéciale pour le débogage
                System.Diagnostics.Debug.WriteLine("ATTENTION: Fenêtre proche du bord droit - Forçage position gauche");

                return candidates;
            }

            // Logique de décision basée sur les bords proches et l'espace disponible
            if (nearEdges == ScreenEdge.None)
            {
                // Fenêtre au centre de l'écran
                if (preferHorizontal)
                {
                    // Priorité à l'axe horizontal
                    if (fitsRight)
                    {
                        candidates.Add(RelativePosition.RightAligned); // Droite alignée (même position verticale)
                        candidates.Add(RelativePosition.Right);        // Droite en haut
                    }

                    if (fitsLeft)
                    {
                        candidates.Add(RelativePosition.LeftAligned);  // Gauche alignée
                        candidates.Add(RelativePosition.Left);         // Gauche en haut
                    }

                    if (fitsBottom)
                    {
                        candidates.Add(RelativePosition.BottomAligned); // Dessous alignée
                        candidates.Add(RelativePosition.Bottom);        // Dessous à gauche
                    }

                    if (fitsTop)
                    {
                        candidates.Add(RelativePosition.TopAligned);   // Dessus alignée 
                        candidates.Add(RelativePosition.Top);          // Dessus à gauche
                    }
                }
                else
                {
                    // Priorité à l'axe vertical
                    if (fitsBottom)
                    {
                        candidates.Add(RelativePosition.BottomAligned); // Dessous alignée
                        candidates.Add(RelativePosition.Bottom);        // Dessous à gauche
                    }

                    if (fitsTop)
                    {
                        candidates.Add(RelativePosition.TopAligned);   // Dessus alignée 
                        candidates.Add(RelativePosition.Top);          // Dessus à gauche
                    }

                    if (fitsRight)
                    {
                        candidates.Add(RelativePosition.RightAligned); // Droite alignée
                        candidates.Add(RelativePosition.Right);        // Droite en haut
                    }

                    if (fitsLeft)
                    {
                        candidates.Add(RelativePosition.LeftAligned);  // Gauche alignée
                        candidates.Add(RelativePosition.Left);         // Gauche en haut
                    }
                }
            }
            else if ((nearEdges & ScreenEdge.Left) != 0)
            {
                // Fenêtre proche du bord gauche -> préférer la droite
                if (preferHorizontal)
                {
                    // Priorité à droite
                    if (fitsRight)
                    {
                        candidates.Add(RelativePosition.RightAligned); // Droite alignée (même position verticale)
                        candidates.Add(RelativePosition.Right);        // Droite en haut
                    }

                    // Puis options verticales
                    if (fitsBottom)
                    {
                        candidates.Add(RelativePosition.BottomAligned); // Dessous alignée
                        candidates.Add(RelativePosition.Bottom);        // Dessous à gauche
                    }

                    if (fitsTop)
                    {
                        candidates.Add(RelativePosition.TopAligned);   // Dessus alignée
                        candidates.Add(RelativePosition.Top);          // Dessus à gauche
                    }
                }
                else
                {
                    // Priorité aux options verticales
                    if (fitsBottom)
                    {
                        candidates.Add(RelativePosition.BottomAligned); // Dessous alignée
                        candidates.Add(RelativePosition.Bottom);        // Dessous à gauche
                    }

                    if (fitsTop)
                    {
                        candidates.Add(RelativePosition.TopAligned);   // Dessus alignée
                        candidates.Add(RelativePosition.Top);          // Dessus à gauche
                    }

                    // Puis à droite
                    if (fitsRight)
                    {
                        candidates.Add(RelativePosition.RightAligned); // Droite alignée (même position verticale)
                        candidates.Add(RelativePosition.Right);        // Droite en haut
                    }
                }

                // Coins si les autres positions ne sont pas disponibles
                if (fitsBottom && fitsRight) candidates.Add(RelativePosition.BottomRight);
                if (fitsTop && fitsRight) candidates.Add(RelativePosition.TopRight);

                // En dernier recours (peu probable que ça tienne)
                if (fitsLeft)
                {
                    candidates.Add(RelativePosition.LeftAligned);
                    candidates.Add(RelativePosition.Left);
                }
            }
            else if ((nearEdges & ScreenEdge.Bottom) != 0)
            {
                // Fenêtre proche du bord inférieur
                if (preferHorizontal)
                {
                    // Priorité à l'horizontal même si près du bord inférieur
                    if (fitsRight && space.Right > space.Left)
                    {
                        candidates.Add(RelativePosition.RightAligned); // Droite alignée
                        candidates.Add(RelativePosition.Right);        // Droite en haut
                    }
                    else if (fitsLeft)
                    {
                        candidates.Add(RelativePosition.LeftAligned);  // Gauche alignée
                        candidates.Add(RelativePosition.Left);         // Gauche en haut
                    }
                    else if (fitsRight) // Si gauche ne tient pas mais droite oui
                    {
                        candidates.Add(RelativePosition.RightAligned); // Droite alignée
                        candidates.Add(RelativePosition.Right);        // Droite en haut
                    }

                    // Ensuite position verticale (au-dessus)
                    if (fitsTop)
                    {
                        candidates.Add(RelativePosition.TopAligned);   // Dessus alignée
                        candidates.Add(RelativePosition.Top);          // Dessus à gauche
                    }
                }
                else
                {
                    // Priorité au-dessus quand près du bord inférieur
                    if (fitsTop)
                    {
                        candidates.Add(RelativePosition.TopAligned);   // Dessus alignée
                        candidates.Add(RelativePosition.Top);          // Dessus à gauche
                    }

                    // Puis horizontal
                    if (fitsRight)
                    {
                        candidates.Add(RelativePosition.RightAligned); // Droite alignée
                        candidates.Add(RelativePosition.Right);        // Droite en haut
                    }

                    if (fitsLeft)
                    {
                        candidates.Add(RelativePosition.LeftAligned);  // Gauche alignée
                        candidates.Add(RelativePosition.Left);         // Gauche en haut
                    }
                }

                // Coins si les autres positions ne sont pas disponibles
                if (fitsTop && fitsRight) candidates.Add(RelativePosition.TopRight);
                if (fitsTop && fitsLeft) candidates.Add(RelativePosition.TopLeft);

                // En dernier recours (peu probable que ça tienne)
                if (fitsBottom)
                {
                    candidates.Add(RelativePosition.BottomAligned);
                    candidates.Add(RelativePosition.Bottom);
                }
            }
            else if ((nearEdges & ScreenEdge.Top) != 0)
            {
                // Fenêtre proche du bord supérieur
                if (preferHorizontal)
                {
                    // Priorité à l'horizontal même si près du bord supérieur
                    if (fitsRight && space.Right > space.Left)
                    {
                        candidates.Add(RelativePosition.RightAligned); // Droite alignée
                        candidates.Add(RelativePosition.Right);        // Droite en haut
                    }
                    else if (fitsLeft)
                    {
                        candidates.Add(RelativePosition.LeftAligned);  // Gauche alignée
                        candidates.Add(RelativePosition.Left);         // Gauche en haut
                    }
                    else if (fitsRight) // Si gauche ne tient pas mais droite oui
                    {
                        candidates.Add(RelativePosition.RightAligned); // Droite alignée
                        candidates.Add(RelativePosition.Right);        // Droite en haut
                    }

                    // Ensuite position verticale (en dessous)
                    if (fitsBottom)
                    {
                        candidates.Add(RelativePosition.BottomAligned); // Dessous alignée
                        candidates.Add(RelativePosition.Bottom);        // Dessous à gauche
                    }
                }
                else
                {
                    // Priorité en-dessous quand près du bord supérieur
                    if (fitsBottom)
                    {
                        candidates.Add(RelativePosition.BottomAligned); // Dessous alignée
                        candidates.Add(RelativePosition.Bottom);        // Dessous à gauche
                    }

                    // Puis horizontal
                    if (fitsRight)
                    {
                        candidates.Add(RelativePosition.RightAligned); // Droite alignée
                        candidates.Add(RelativePosition.Right);        // Droite en haut
                    }

                    if (fitsLeft)
                    {
                        candidates.Add(RelativePosition.LeftAligned);  // Gauche alignée
                        candidates.Add(RelativePosition.Left);         // Gauche en haut
                    }
                }

                // Coins si les autres positions ne sont pas disponibles
                if (fitsBottom && fitsRight) candidates.Add(RelativePosition.BottomRight);
                if (fitsBottom && fitsLeft) candidates.Add(RelativePosition.BottomLeft);

                // En dernier recours (peu probable que ça tienne)
                if (fitsTop)
                {
                    candidates.Add(RelativePosition.TopAligned);
                    candidates.Add(RelativePosition.Top);
                }
            }

            // Cas des coins (plusieurs bords proches à la fois) - maintenir logique existante
            if ((nearEdges & (ScreenEdge.Right | ScreenEdge.Bottom)) == (ScreenEdge.Right | ScreenEdge.Bottom))
            {
                // Coin inférieur droit -> préférer le coin supérieur gauche
                candidates.Clear();

                if (fitsTop && fitsLeft) candidates.Add(RelativePosition.TopLeft);
                if (fitsTop) candidates.Add(RelativePosition.TopAligned);
                if (fitsLeft) candidates.Add(RelativePosition.LeftAligned);
                if (fitsTop) candidates.Add(RelativePosition.Top);
                if (fitsLeft) candidates.Add(RelativePosition.Left);

                // Autres options si aucune des précédentes ne fonctionne
                if (fitsBottom && fitsLeft) candidates.Add(RelativePosition.BottomLeft);
                if (fitsRight && fitsTop) candidates.Add(RelativePosition.TopRight);
            }
            else if ((nearEdges & (ScreenEdge.Left | ScreenEdge.Bottom)) == (ScreenEdge.Left | ScreenEdge.Bottom))
            {
                // Coin inférieur gauche -> préférer le coin supérieur droit
                candidates.Clear();

                if (fitsTop && fitsRight) candidates.Add(RelativePosition.TopRight);
                if (fitsTop) candidates.Add(RelativePosition.TopAligned);
                if (fitsRight) candidates.Add(RelativePosition.RightAligned);
                if (fitsTop) candidates.Add(RelativePosition.Top);
                if (fitsRight) candidates.Add(RelativePosition.Right);

                // Autres options si aucune des précédentes ne fonctionne
                if (fitsBottom && fitsRight) candidates.Add(RelativePosition.BottomRight);
                if (fitsLeft && fitsTop) candidates.Add(RelativePosition.TopLeft);
            }
            else if ((nearEdges & (ScreenEdge.Right | ScreenEdge.Top)) == (ScreenEdge.Right | ScreenEdge.Top))
            {
                // Coin supérieur droit -> préférer le coin inférieur gauche
                candidates.Clear();

                if (fitsBottom && fitsLeft) candidates.Add(RelativePosition.BottomLeft);
                if (fitsBottom) candidates.Add(RelativePosition.BottomAligned);
                if (fitsLeft) candidates.Add(RelativePosition.LeftAligned);
                if (fitsBottom) candidates.Add(RelativePosition.Bottom);
                if (fitsLeft) candidates.Add(RelativePosition.Left);

                // Autres options si aucune des précédentes ne fonctionne
                if (fitsTop && fitsLeft) candidates.Add(RelativePosition.TopLeft);
                if (fitsRight && fitsBottom) candidates.Add(RelativePosition.BottomRight);
            }
            else if ((nearEdges & (ScreenEdge.Left | ScreenEdge.Top)) == (ScreenEdge.Left | ScreenEdge.Top))
            {
                // Coin supérieur gauche -> préférer le coin inférieur droit
                candidates.Clear();

                if (fitsBottom && fitsRight) candidates.Add(RelativePosition.BottomRight);
                if (fitsBottom) candidates.Add(RelativePosition.BottomAligned);
                if (fitsRight) candidates.Add(RelativePosition.RightAligned);
                if (fitsBottom) candidates.Add(RelativePosition.Bottom);
                if (fitsRight) candidates.Add(RelativePosition.Right);

                // Autres options si aucune des précédentes ne fonctionne
                if (fitsTop && fitsRight) candidates.Add(RelativePosition.TopRight);
                if (fitsLeft && fitsBottom) candidates.Add(RelativePosition.BottomLeft);
            }

            // Ajouter toutes les autres positions possibles si elles ne sont pas déjà présentes
            // pour garantir qu'on a toujours des options
            foreach (RelativePosition position in Enum.GetValues(typeof(RelativePosition)))
            {
                if (!candidates.Contains(position))
                {
                    candidates.Add(position);
                }
            }

            // Journaliser les positions candidates pour le débogage
            System.Diagnostics.Debug.WriteLine("Positions candidates par ordre de priorité:");
            for (int i = 0; i < candidates.Count; i++)
            {
                System.Diagnostics.Debug.WriteLine($"{i + 1}. {candidates[i]}");
            }

            return candidates;
        }

        /// <summary>
        /// Obtient la meilleure position visible parmi les candidats en testant différents seuils de visibilité.
        /// Version améliorée qui tient compte des préférences horizontales/verticales.
        /// </summary>
        /// <param name="originalPosition">Position de la fenêtre principale</param>
        /// <param name="originalSize">Taille de la fenêtre principale</param>
        /// <param name="previewSize">Taille de la fenêtre de prévisualisation</param>
        /// <param name="candidates">Liste des positions candidates</param>
        /// <param name="screens">Liste des écrans disponibles</param>
        /// <returns>Meilleure position trouvée</returns>
        private Point GetBestVisiblePosition(
            Point originalPosition,
            Size originalSize,
            Size previewSize,
            List<RelativePosition> candidates,
            IEnumerable<HelloWorld.ScreenUtility.MonitorInfo> screens)
        {
            // Calculer les dimensions de l'espace de travail
            Rect originalWindowRect = new Rect(originalPosition, originalSize);
            HelloWorld.ScreenUtility.MonitorInfo targetScreen = WindowPositioningHelper.FindBestMonitorForRect(originalWindowRect);
            AvailableSpace availableSpace = CalculateAvailableSpace(originalWindowRect, targetScreen);

            // Déterminer s'il faut privilégier l'horizontal
            bool preferHorizontal = IsHorizontalSpaceProportionallyBetter(availableSpace, previewSize);
            System.Diagnostics.Debug.WriteLine($"Évaluation des positions: préférence horizontale = {preferHorizontal}");

            // Tester chaque seuil de visibilité, du plus strict au plus permissif
            foreach (double threshold in VISIBILITY_THRESHOLDS)
            {
                // Liste pour stocker les positions viables avec leur score
                var viablePositions = new Dictionary<Point, double>();

                // Tester chaque position candidate
                foreach (var position in candidates)
                {
                    // Calculer les coordonnées de la position candidate
                    Point candidatePosition = CalculateCoordinates(
                        originalPosition, originalSize, previewSize, position);

                    // Créer un rectangle représentant la fenêtre de prévisualisation à cette position
                    Rect previewRect = new Rect(candidatePosition, previewSize);

                    // Vérifier si la position est suffisamment visible
                    if (WindowPositioningHelper.IsRectanglePartiallyVisible(previewRect, threshold))
                    {
                        // Calculer un score pour cette position en fonction des préférences
                        double score = 1.0; // Score de base

                        // Favoriser les positions horizontales ou verticales selon la préférence
                        if (preferHorizontal)
                        {
                            if (position == RelativePosition.Left || position == RelativePosition.LeftAligned ||
                                position == RelativePosition.Right || position == RelativePosition.RightAligned)
                            {
                                score *= 1.5; // Bonus de 50% pour les positions horizontales
                            }
                        }
                        else
                        {
                            if (position == RelativePosition.Top || position == RelativePosition.TopAligned ||
                                position == RelativePosition.Bottom || position == RelativePosition.BottomAligned)
                            {
                                score *= 1.5; // Bonus de 50% pour les positions verticales
                            }
                        }

                        // Si la fenêtre est près du bord droit, bonus supplémentaire pour la position gauche
                        if ((targetScreen != null) && (originalWindowRect.Right > targetScreen.Bounds.Right - targetScreen.Width * 0.20))
                        {
                            if (position == RelativePosition.Left || position == RelativePosition.LeftAligned)
                            {
                                score *= 2.0; // Bonus de 100% pour la position gauche quand près du bord droit
                            }
                        }

                        viablePositions[candidatePosition] = score;
                    }
                }

                // Si on a trouvé des positions viables à ce niveau de seuil, retourner la meilleure
                if (viablePositions.Count > 0)
                {
                    var bestPosition = viablePositions.OrderByDescending(p => p.Value).First().Key;
                    System.Diagnostics.Debug.WriteLine($"Position choisie: {candidates[viablePositions.Keys.ToList().IndexOf(bestPosition)]} " +
                                                   $"({bestPosition.X},{bestPosition.Y}) avec seuil de visibilité {threshold:P0}");
                    return bestPosition;
                }
            }

            // Si aucune position n'est suffisamment visible, utiliser la première position candidate
            // et laisser la contrainte finale s'occuper de maintenir la visibilité
            System.Diagnostics.Debug.WriteLine("Aucune position n'est suffisamment visible, utilisation de la première position candidate");
            Point defaultPosition = CalculateCoordinates(
                originalPosition, originalSize, previewSize, candidates.First());

            // Vérifier que cette position reste dans les limites de l'écran
            Rect positionRect = new Rect(defaultPosition, previewSize);
            Rect constrainedRect = WindowPositioningHelper.ConstrainRectToScreen(positionRect);

            return constrainedRect.TopLeft;
        }

        /// <summary>
        /// Calcule les coordonnées de la fenêtre de prévisualisation pour une position relative donnée.
        /// Les positions alignées conservent l'alignement vertical ou horizontal avec la fenêtre principale.
        /// </summary>
        /// <param name="originalPosition">Position de la fenêtre principale</param>
        /// <param name="originalSize">Taille de la fenêtre principale</param>
        /// <param name="previewSize">Taille de la fenêtre de prévisualisation</param>
        /// <param name="position">Position relative</param>
        /// <returns>Coordonnées calculées</returns>
        private Point CalculateCoordinates(
            Point originalPosition,
            Size originalSize,
            Size previewSize,
            RelativePosition position)
        {
            // Calculer la position du centre de la fenêtre principale
            double centerX = originalPosition.X + originalSize.Width / 2;
            double centerY = originalPosition.Y + originalSize.Height / 2;

            switch (position)
            {
                case RelativePosition.Right:
                    // À droite, aligné en haut
                    return new Point(
                        originalPosition.X + originalSize.Width + SPACING,
                        originalPosition.Y);

                case RelativePosition.Left:
                    // À gauche, aligné en haut
                    return new Point(
                        originalPosition.X - previewSize.Width - SPACING,
                        originalPosition.Y);

                case RelativePosition.Bottom:
                    // En dessous, aligné à gauche
                    return new Point(
                        originalPosition.X,
                        originalPosition.Y + originalSize.Height + SPACING);

                case RelativePosition.Top:
                    // Au-dessus, aligné à gauche
                    return new Point(
                        originalPosition.X,
                        originalPosition.Y - previewSize.Height - SPACING);

                case RelativePosition.RightAligned:
                    // À droite, aligné verticalement sur le centre de la fenêtre principale
                    return new Point(
                        originalPosition.X + originalSize.Width + SPACING,
                        centerY - previewSize.Height / 2);

                case RelativePosition.LeftAligned:
                    // À gauche, aligné verticalement sur le centre de la fenêtre principale
                    return new Point(
                        originalPosition.X - previewSize.Width - SPACING,
                        centerY - previewSize.Height / 2);

                case RelativePosition.BottomAligned:
                    // En dessous, aligné horizontalement sur le centre de la fenêtre principale
                    return new Point(
                        centerX - previewSize.Width / 2,
                        originalPosition.Y + originalSize.Height + SPACING);

                case RelativePosition.TopAligned:
                    // Au-dessus, aligné horizontalement sur le centre de la fenêtre principale
                    return new Point(
                        centerX - previewSize.Width / 2,
                        originalPosition.Y - previewSize.Height - SPACING);

                case RelativePosition.BottomRight:
                    // Diagonale bas-droite
                    return new Point(
                        originalPosition.X + originalSize.Width + SPACING,
                        originalPosition.Y + originalSize.Height + SPACING);

                case RelativePosition.BottomLeft:
                    // Diagonale bas-gauche
                    return new Point(
                        originalPosition.X - previewSize.Width - SPACING,
                        originalPosition.Y + originalSize.Height + SPACING);

                case RelativePosition.TopRight:
                    // Diagonale haut-droite
                    return new Point(
                        originalPosition.X + originalSize.Width + SPACING,
                        originalPosition.Y - previewSize.Height - SPACING);

                case RelativePosition.TopLeft:
                    // Diagonale haut-gauche
                    return new Point(
                        originalPosition.X - previewSize.Width - SPACING,
                        originalPosition.Y - previewSize.Height - SPACING);

                default:
                    // Position par défaut en cas d'erreur (à droite)
                    return new Point(
                        originalPosition.X + originalSize.Width + SPACING,
                        originalPosition.Y);
            }
        }

        #endregion
    }
}