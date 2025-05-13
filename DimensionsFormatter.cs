using System;
using System.Windows;

namespace HelloWorld.Preview
{
    /// <summary>
    /// Classe utilitaire pour formater l'affichage des dimensions selon différents types d'indicateurs.
    /// Centralise la logique de formatage pour éviter la duplication de code entre les différents renderers.
    /// </summary>
    public static class DimensionsFormatter
    {
        /// <summary>
        /// Génère le texte de dimensions à afficher avec un format lisible
        /// selon le type d'indicateur sélectionné, en séparant les pixels et le pourcentage sur deux lignes si nécessaire.
        /// </summary>
        /// <param name="width">Largeur en pixels</param>
        /// <param name="height">Hauteur en pixels</param>
        /// <param name="indicatorType">Type d'indicateur à utiliser</param>
        /// <param name="includeLabel">Indique si le texte doit inclure un label "Dimensions:" en préfixe</param>
        /// <returns>Le texte formaté des dimensions</returns>
        public static string FormatDimensions(double width, double height, DimensionIndicatorType indicatorType, bool includeLabel = false)
        {
            // Vérifier les paramètres d'entrée
            if (width <= 0 || height <= 0)
            {
                return "Dimensions invalides";
            }

            try
            {
                // Préfixe à ajouter si demandé
                string prefix = includeLabel ? "Dimensions: " : "";

                // Formater les dimensions avec des entiers (arrondir pour éviter des valeurs décimales)
                int roundedWidth = (int)Math.Round(width);
                int roundedHeight = (int)Math.Round(height);

                // Déterminer le format selon le type d'indicateur
                switch (indicatorType)
                {
                    case DimensionIndicatorType.PixelsOnly:
                        // Format par défaut: uniquement les pixels
                        return $"{prefix}{roundedWidth} × {roundedHeight}";

                    case DimensionIndicatorType.PixelsAndPercentage:
                        // Format sur deux lignes: pixels et pourcentage de l'écran
                        double screenWidthPercentage = 0;
                        double screenHeightPercentage = 0;

                        // Calculer le pourcentage par rapport à l'écran principal
                        try
                        {
                            // Obtenir les dimensions de l'écran principal
                            var primaryScreen = HelloWorld.ScreenUtility.PrimaryMonitor;
                            if (primaryScreen != null)
                            {
                                // Obtenir le facteur d'échelle DPI
                                double dpiScaleFactor = WindowPositioningHelper.GetDpiScaleFactor(primaryScreen);

                                // Convertir les dimensions logiques en dimensions physiques
                                double physicalWidth = width * dpiScaleFactor;
                                double physicalHeight = height * dpiScaleFactor;

                                // Calculer les pourcentages
                                screenWidthPercentage = (physicalWidth / primaryScreen.Width) * 100;
                                screenHeightPercentage = (physicalHeight / primaryScreen.Height) * 100;

                                // Limiter à deux décimales
                                screenWidthPercentage = Math.Round(screenWidthPercentage, 2);
                                screenHeightPercentage = Math.Round(screenHeightPercentage, 2);
                            }
                        }
                        catch (Exception ex)
                        {
                            // En cas d'erreur, journaliser et continuer
                            System.Diagnostics.Debug.WriteLine($"Erreur lors du calcul des pourcentages d'écran: {ex.Message}");
                        }

                        // Retourner le format sur deux lignes avec un saut de ligne
                        return $"{prefix}{roundedWidth} × {roundedHeight}\n({screenWidthPercentage}% × {screenHeightPercentage}%)";

                    case DimensionIndicatorType.PercentageOnly:
                        // Format pourcentage uniquement
                        double widthPercentage = 0;
                        double heightPercentage = 0;

                        // Calculer le pourcentage par rapport à l'écran principal
                        try
                        {
                            // Obtenir les dimensions de l'écran principal
                            var primaryScreen = HelloWorld.ScreenUtility.PrimaryMonitor;
                            if (primaryScreen != null)
                            {
                                // Obtenir le facteur d'échelle DPI
                                double dpiScaleFactor = WindowPositioningHelper.GetDpiScaleFactor(primaryScreen);

                                // Convertir les dimensions logiques en dimensions physiques
                                double physicalWidth = width * dpiScaleFactor;
                                double physicalHeight = height * dpiScaleFactor;

                                // Calculer les pourcentages
                                widthPercentage = (physicalWidth / primaryScreen.Width) * 100;
                                heightPercentage = (physicalHeight / primaryScreen.Height) * 100;

                                // Limiter à deux décimales
                                widthPercentage = Math.Round(widthPercentage, 2);
                                heightPercentage = Math.Round(heightPercentage, 2);
                            }
                        }
                        catch (Exception ex)
                        {
                            // En cas d'erreur, journaliser et continuer
                            System.Diagnostics.Debug.WriteLine($"Erreur lors du calcul des pourcentages d'écran: {ex.Message}");
                        }

                        // Retourner le format pourcentage uniquement
                        return $"{prefix}{widthPercentage}% × {heightPercentage}%";

                    default:
                        // En cas de valeur inconnue, utiliser le format par défaut
                        return $"{prefix}{roundedWidth} × {roundedHeight}";
                }
            }
            catch (Exception ex)
            {
                // En cas d'erreur inattendue, journaliser et retourner un message d'erreur générique
                System.Diagnostics.Debug.WriteLine($"Erreur inattendue lors du formatage des dimensions: {ex.Message}");
                return "Erreur de formatage";
            }
        }
    }
}