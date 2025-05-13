using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using HelloWorld.Preview;

namespace HelloWorld.Preview
{
    /// <summary>
    /// Fournit des méthodes d'extension pour faciliter l'utilisation de WindowPreviewManager
    /// dans différents contextes d'application.
    /// </summary>
    public static class WindowPreviewManagerExtensions
    {
        #region Extensions pour Window

        /// <summary>
        /// Active la prévisualisation pour une fenêtre avec les paramètres spécifiés
        /// </summary>
        /// <param name="window">Fenêtre à prévisualiser</param>
        /// <param name="rendererType">Type de renderer à utiliser</param>
        /// <param name="strategyType">Type de stratégie de positionnement à utiliser</param>
        /// <returns>Gestionnaire de prévisualisation configuré</returns>
        public static IWindowPreviewManager EnablePreview(
            this Window window,
            PreviewRendererType rendererType = PreviewRendererType.Simplified,
            PositionStrategyType strategyType = PositionStrategyType.Snap)
        {
            // Créer et configurer un nouveau gestionnaire de prévisualisation
            IWindowPreviewManager previewManager = new WindowPreviewManager();

            // Initialiser le gestionnaire avec la fenêtre
            previewManager.Initialize(window);

            // Configurer le renderer
            IPreviewRenderer renderer = PreviewRendererFactory.CreateRenderer(rendererType);
            previewManager.SetPreviewRenderer(renderer);

            // Configurer la stratégie de positionnement
            IPositionStrategy strategy = CreatePositionStrategy(strategyType);
            previewManager.SetPositionStrategy(strategy);

            // Stocker le gestionnaire dans les propriétés de la fenêtre pour y accéder plus tard
            window.SetValue(PreviewManagerProperty, previewManager);

            return previewManager;
        }

        /// <summary>
        /// Désactive la prévisualisation pour une fenêtre
        /// </summary>
        /// <param name="window">Fenêtre dont la prévisualisation doit être désactivée</param>
        public static void DisablePreview(this Window window)
        {
            // Récupérer le gestionnaire de prévisualisation
            IWindowPreviewManager previewManager = window.GetValue(PreviewManagerProperty) as IWindowPreviewManager;

            if (previewManager != null)
            {
                // Arrêter la prévisualisation active
                if (previewManager.IsPreviewActive)
                {
                    previewManager.StopPreview();
                }

                // Nettoyer les ressources
                if (previewManager is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                // Supprimer la référence au gestionnaire
                window.ClearValue(PreviewManagerProperty);
            }
        }

        /// <summary>
        /// Obtient le gestionnaire de prévisualisation associé à une fenêtre
        /// </summary>
        /// <param name="window">Fenêtre dont on veut obtenir le gestionnaire</param>
        /// <returns>Gestionnaire de prévisualisation ou null si aucun n'est associé</returns>
        public static IWindowPreviewManager GetPreviewManager(this Window window)
        {
            return window.GetValue(PreviewManagerProperty) as IWindowPreviewManager;
        }

        /// <summary>
        /// Crée une prévisualisation des dimensions spécifiées pour une fenêtre
        /// </summary>
        /// <param name="window">Fenêtre à prévisualiser</param>
        /// <param name="width">Largeur à prévisualiser</param>
        /// <param name="height">Hauteur à prévisualiser</param>
        public static void PreviewDimensions(this Window window, double width, double height)
        {
            // Récupérer le gestionnaire de prévisualisation
            IWindowPreviewManager previewManager = window.GetValue(PreviewManagerProperty) as IWindowPreviewManager;

            if (previewManager == null)
            {
                // Créer un nouveau gestionnaire si aucun n'existe
                previewManager = window.EnablePreview();
            }

            // Démarrer ou mettre à jour la prévisualisation
            if (previewManager.IsPreviewActive)
            {
                previewManager.UpdatePreview(new Size(width, height));
            }
            else
            {
                previewManager.StartPreview(new Size(width, height));
            }
        }

        /// <summary>
        /// Applique les dimensions prévisualisées à une fenêtre
        /// </summary>
        /// <param name="window">Fenêtre à redimensionner</param>
        /// <returns>True si les dimensions ont été appliquées, sinon False</returns>
        public static bool ApplyPreviewedDimensions(this Window window)
        {
            // Récupérer le gestionnaire de prévisualisation
            IWindowPreviewManager previewManager = window.GetValue(PreviewManagerProperty) as IWindowPreviewManager;

            if (previewManager != null && previewManager.IsPreviewActive)
            {
                // Appliquer les dimensions prévisualisées
                previewManager.ApplyPreviewedDimensions();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Propriété attachée pour stocker le gestionnaire de prévisualisation
        /// </summary>
        public static readonly DependencyProperty PreviewManagerProperty =
            DependencyProperty.RegisterAttached(
                "PreviewManager",
                typeof(IWindowPreviewManager),
                typeof(WindowPreviewManagerExtensions),
                new PropertyMetadata(null));

        #endregion

        #region Extensions pour les contrôles d'entrée (TextBox, Slider)

        /// <summary>
        /// Connecte des TextBox de dimensions à une fenêtre pour la prévisualisation
        /// </summary>
        /// <param name="window">Fenêtre à prévisualiser</param>
        /// <param name="widthTextBox">TextBox pour la largeur</param>
        /// <param name="heightTextBox">TextBox pour la hauteur</param>
        /// <param name="livePreview">Indique si la prévisualisation doit être mise à jour en temps réel</param>
        /// <returns>Gestionnaire de prévisualisation configuré</returns>
        public static IWindowPreviewManager ConnectDimensionControls(
            this Window window,
            TextBox widthTextBox,
            TextBox heightTextBox,
            bool livePreview = true)
        {
            // Créer un fournisseur de dimensions basé sur les TextBox
            IWindowDimensionProvider provider = new ControlBasedDimensionProvider(widthTextBox, heightTextBox);

            // Connecter le fournisseur à la fenêtre
            return ConnectDimensionProvider(window, provider, livePreview);
        }

        /// <summary>
        /// Connecte des Slider de dimensions à une fenêtre pour la prévisualisation
        /// </summary>
        /// <param name="window">Fenêtre à prévisualiser</param>
        /// <param name="widthSlider">Slider pour la largeur</param>
        /// <param name="heightSlider">Slider pour la hauteur</param>
        /// <param name="livePreview">Indique si la prévisualisation doit être mise à jour en temps réel</param>
        /// <returns>Gestionnaire de prévisualisation configuré</returns>
        public static IWindowPreviewManager ConnectDimensionControls(
            this Window window,
            Slider widthSlider,
            Slider heightSlider,
            bool livePreview = true)
        {
            // Créer un fournisseur de dimensions basé sur les Slider
            IWindowDimensionProvider provider = new ControlBasedDimensionProvider(widthSlider, heightSlider);

            // Connecter le fournisseur à la fenêtre
            return ConnectDimensionProvider(window, provider, livePreview);
        }

        /// <summary>
        /// Connecte des contrôles de dimensions à une fenêtre pour la prévisualisation
        /// </summary>
        /// <param name="window">Fenêtre à prévisualiser</param>
        /// <param name="widthTextBox">TextBox pour la largeur</param>
        /// <param name="heightTextBox">TextBox pour la hauteur</param>
        /// <param name="widthSlider">Slider pour la largeur</param>
        /// <param name="heightSlider">Slider pour la hauteur</param>
        /// <param name="livePreview">Indique si la prévisualisation doit être mise à jour en temps réel</param>
        /// <returns>Gestionnaire de prévisualisation configuré</returns>
        public static IWindowPreviewManager ConnectDimensionControls(
            this Window window,
            TextBox widthTextBox,
            TextBox heightTextBox,
            Slider widthSlider,
            Slider heightSlider,
            bool livePreview = true)
        {
            // Créer un fournisseur de dimensions basé sur les TextBox et les Slider
            IWindowDimensionProvider provider = new ControlBasedDimensionProvider(
                widthTextBox, heightTextBox, widthSlider, heightSlider);

            // Connecter le fournisseur à la fenêtre
            return ConnectDimensionProvider(window, provider, livePreview);
        }

        /// <summary>
        /// Connecte un fournisseur de dimensions à une fenêtre pour la prévisualisation
        /// </summary>
        /// <param name="window">Fenêtre à prévisualiser</param>
        /// <param name="provider">Fournisseur de dimensions</param>
        /// <param name="livePreview">Indique si la prévisualisation doit être mise à jour en temps réel</param>
        /// <returns>Gestionnaire de prévisualisation configuré</returns>
        public static IWindowPreviewManager ConnectDimensionProvider(
            this Window window,
            IWindowDimensionProvider provider,
            bool livePreview = true)
        {
            // Récupérer ou créer le gestionnaire de prévisualisation
            IWindowPreviewManager previewManager = window.GetValue(PreviewManagerProperty) as IWindowPreviewManager;

            if (previewManager == null)
            {
                // Créer un nouveau gestionnaire si aucun n'existe
                previewManager = window.EnablePreview();
            }

            // Configurer le fournisseur de dimensions
            previewManager.SetDimensionProvider(provider);

            if (livePreview)
            {
                // S'abonner à l'événement de changement de dimensions
                provider.DimensionsChanged += (sender, e) =>
                {
                    // Démarrer ou mettre à jour la prévisualisation lorsque les dimensions changent
                    if (previewManager.IsPreviewActive)
                    {
                        previewManager.UpdatePreview(e.NewSize);
                    }
                    else
                    {
                        previewManager.StartPreview(e.NewSize);
                    }
                };
            }

            return previewManager;
        }

        #endregion

        #region Extensions pour les boutons d'action

        /// <summary>
        /// Connecte un bouton standard à une fenêtre pour la prévisualisation
        /// </summary>
        /// <param name="window">Fenêtre à prévisualiser</param>
        /// <param name="button">Bouton standard</param>
        /// <param name="provider">Fournisseur de dimensions optionnel</param>
        /// <returns>Gestionnaire de prévisualisation configuré</returns>
        public static IWindowPreviewManager ConnectStandardButton(
            this Window window,
            Button button,
            IWindowDimensionProvider provider = null)
        {
            // Récupérer ou créer le gestionnaire de prévisualisation
            IWindowPreviewManager previewManager = window.GetValue(PreviewManagerProperty) as IWindowPreviewManager;

            if (previewManager == null)
            {
                // Créer un nouveau gestionnaire si aucun n'existe
                previewManager = window.EnablePreview();
            }

            // Configurer le fournisseur de dimensions si fourni
            if (provider != null)
            {
                previewManager.SetDimensionProvider(provider);
            }

            // Contenu initial du bouton
            object originalContent = button.Content;

            // S'abonner à l'événement Click du bouton
            button.Click += (sender, e) =>
            {
                // Obtenir les dimensions actuelles
                Size currentDimensions;

                if (provider != null)
                {
                    currentDimensions = provider.GetCurrentDimensions();
                }
                else
                {
                    currentDimensions = new Size(window.Width, window.Height);
                }

                // Démarrer ou mettre à jour la prévisualisation
                if (previewManager.IsPreviewActive)
                {
                    previewManager.UpdatePreview(currentDimensions);
                }
                else
                {
                    button.Content = "Arrêter la prévisualisation";
                    previewManager.StartPreview(currentDimensions);
                }
            };

            // S'abonner à l'événement PreviewStopped pour mettre à jour le bouton
            previewManager.PreviewStopped += (sender, e) =>
            {
                button.Content = originalContent ?? "Prévisualiser";
            };

            return previewManager;
        }

        /// <summary>
        /// Connecte un bouton Apply standard à une fenêtre pour appliquer les dimensions prévisualisées
        /// </summary>
        /// <param name="window">Fenêtre à redimensionner</param>
        /// <param name="button">Bouton d'application</param>
        /// <returns>Gestionnaire de prévisualisation configuré</returns>
        public static IWindowPreviewManager ConnectStandardApplyButton(
            this Window window,
            Button button)
        {
            // Récupérer ou créer le gestionnaire de prévisualisation
            IWindowPreviewManager previewManager = window.GetValue(PreviewManagerProperty) as IWindowPreviewManager;

            if (previewManager == null)
            {
                // Créer un nouveau gestionnaire si aucun n'existe
                previewManager = window.EnablePreview();
            }

            // S'abonner à l'événement Click du bouton
            button.Click += (sender, e) =>
            {
                // Appliquer les dimensions prévisualisées si une prévisualisation est active
                if (previewManager.IsPreviewActive)
                {
                    previewManager.ApplyPreviewedDimensions();
                }
            };

            return previewManager;
        }

        /// <summary>
        /// Connecte un bouton de prévisualisation à une fenêtre
        /// </summary>
        /// <param name="window">Fenêtre à prévisualiser</param>
        /// <param name="previewButton">Bouton de prévisualisation</param>
        /// <param name="provider">Fournisseur de dimensions optionnel</param>
        /// <returns>Gestionnaire de prévisualisation configuré</returns>
        public static IWindowPreviewManager ConnectPreviewButton(
            this Window window,
            PreviewButton previewButton,
            IWindowDimensionProvider provider = null)
        {
            // Récupérer ou créer le gestionnaire de prévisualisation
            IWindowPreviewManager previewManager = window.GetValue(PreviewManagerProperty) as IWindowPreviewManager;

            if (previewManager == null)
            {
                // Créer un nouveau gestionnaire si aucun n'existe
                previewManager = window.EnablePreview();
            }

            // Configurer le fournisseur de dimensions si fourni
            if (provider != null)
            {
                previewManager.SetDimensionProvider(provider);
            }

            // S'abonner à l'événement Click du bouton
            previewButton.Click += (sender, e) =>
            {
                // Obtenir les dimensions actuelles
                Size currentDimensions;

                if (provider != null)
                {
                    currentDimensions = provider.GetCurrentDimensions();
                }
                else
                {
                    currentDimensions = new Size(window.Width, window.Height);
                }

                // Démarrer ou mettre à jour la prévisualisation
                if (previewManager.IsPreviewActive)
                {
                    previewManager.UpdatePreview(currentDimensions);
                }
                else
                {
                    previewButton.Content = "Arrêter la prévisualisation";
                    previewManager.StartPreview(currentDimensions);
                }
            };

            // S'abonner à l'événement PreviewStopped pour mettre à jour le bouton
            previewManager.PreviewStopped += (sender, e) =>
            {
                previewButton.Content = "Prévisualiser";
            };

            return previewManager;
        }

        /// <summary>
        /// Connecte un bouton d'application des dimensions à une fenêtre
        /// </summary>
        /// <param name="window">Fenêtre à redimensionner</param>
        /// <param name="applyButton">Bouton d'application</param>
        /// <returns>Gestionnaire de prévisualisation configuré</returns>
        public static IWindowPreviewManager ConnectApplyButton(
            this Window window,
            PreviewButton applyButton)
        {
            // Récupérer ou créer le gestionnaire de prévisualisation
            IWindowPreviewManager previewManager = window.GetValue(PreviewManagerProperty) as IWindowPreviewManager;

            if (previewManager == null)
            {
                // Créer un nouveau gestionnaire si aucun n'existe
                previewManager = window.EnablePreview();
            }

            // S'abonner à l'événement Click du bouton
            applyButton.Click += (sender, e) =>
            {
                // Appliquer les dimensions prévisualisées si une prévisualisation est active
                if (previewManager.IsPreviewActive)
                {
                    previewManager.ApplyPreviewedDimensions();
                }
            };

            return previewManager;
        }

        #endregion

        #region Méthodes utilitaires

        /// <summary>
        /// Crée une stratégie de positionnement en fonction du type spécifié
        /// avec des configurations optimisées pour maximiser la visibilité
        /// </summary>
        /// <param name="strategyType">Type de stratégie à créer</param>
        /// <returns>Stratégie de positionnement</returns>
        private static IPositionStrategy CreatePositionStrategy(PositionStrategyType strategyType)
        {
            switch (strategyType)
            {
                case PositionStrategyType.Adjacent:
                    // Stratégie simple qui place la fenêtre à côté de la fenêtre principale
                    return new AdjacentPositionStrategy();

                case PositionStrategyType.CenterScreen:
                    // Stratégie qui centre la fenêtre sur l'écran contenant la fenêtre principale
                    return new CenterScreenPositionStrategy();

                case PositionStrategyType.Snap:
                    // Utiliser notre nouvelle stratégie intelligente au lieu de SnapPositionStrategy
                    // pour garantir un positionnement optimal dans tous les cas
                    return new SmartPositionStrategy();

                case PositionStrategyType.Dock:
                    // Stratégie d'ancrage avec zones préférées optimisées et contrainte de visibilité
                    var dockStrategy = new DockPositionStrategy(DockPositionStrategy.DockZone.Right, 10,
                        new DockPositionStrategy.DockZone[] {
                    DockPositionStrategy.DockZone.Bottom,
                    DockPositionStrategy.DockZone.Left,
                    DockPositionStrategy.DockZone.Top,
                    DockPositionStrategy.DockZone.BottomRight,
                    DockPositionStrategy.DockZone.BottomLeft,
                    DockPositionStrategy.DockZone.TopRight,
                    DockPositionStrategy.DockZone.TopLeft
                        });
                    return new ConstrainedPositionStrategy(dockStrategy);

                case PositionStrategyType.Memory:
                    // Stratégie qui mémorise la dernière position relative
                    // Utiliser SmartPositionStrategy comme stratégie de secours
                    var smartStrategy = new SmartPositionStrategy();
                    var memoryStrategy = new MemoryPositionStrategy(smartStrategy);
                    return new ConstrainedPositionStrategy(memoryStrategy, new CenterScreenPositionStrategy());

                case PositionStrategyType.Smart:
                    // Utiliser directement notre nouvelle stratégie intelligente
                    return new SmartPositionStrategy();

                default:
                    // Par défaut, utiliser également notre stratégie intelligente
                    // pour garantir un bon positionnement dans tous les cas
                    return new SmartPositionStrategy();
            }
        }

        /// <summary>
        /// Analyse et diagnostique les problèmes potentiels de positionnement du preview
        /// </summary>
        /// <param name="window">Fenêtre à analyser</param>
        /// <returns>Rapport de diagnostic</returns>
        public static string DiagnosePreviewPositioning(this Window window)
        {
            // Construire un générateur de chaînes pour le rapport
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== Diagnostic de positionnement du preview ===");

            try
            {
                // Récupérer le gestionnaire de prévisualisation
                IWindowPreviewManager previewManager = window.GetValue(PreviewManagerProperty) as IWindowPreviewManager;

                // Vérifier si un gestionnaire existe
                if (previewManager == null)
                {
                    report.AppendLine("ERREUR: Aucun gestionnaire de prévisualisation n'est associé à cette fenêtre.");
                    report.AppendLine("Solution: Utilisez la méthode Window.EnablePreview() pour activer la prévisualisation.");
                    return report.ToString();
                }

                // Vérifier si le gestionnaire est initialisé
                if (!previewManager.IsInitialized)
                {
                    report.AppendLine("ERREUR: Le gestionnaire de prévisualisation n'est pas initialisé.");
                    report.AppendLine("Solution: Assurez-vous que la fenêtre cible est valide lors de l'initialisation.");
                    return report.ToString();
                }

                // Obtenir des informations sur les écrans
                var monitors = ScreenUtility.Monitors.ToList();
                report.AppendLine($"Nombre d'écrans détectés: {monitors.Count}");

                // Informations sur chaque écran
                for (int i = 0; i < monitors.Count; i++)
                {
                    var monitor = monitors[i];
                    report.AppendLine($"Écran {i + 1}: {monitor.Width}x{monitor.Height} à ({monitor.Bounds.Left},{monitor.Bounds.Top})");
                    report.AppendLine($"  Principal: {monitor.IsPrimary}");
                }

                // Informations sur la fenêtre principale
                report.AppendLine($"Position de la fenêtre principale: ({window.Left},{window.Top})");
                report.AppendLine($"Dimensions de la fenêtre principale: {window.Width}x{window.Height}");

                // Vérifier sur quel écran se trouve la fenêtre principale
                var windowRect = new Rect(window.Left, window.Top, window.Width, window.Height);
                var windowMonitor = ScreenUtility.FindMonitorContainingWindow(window);

                if (windowMonitor != null)
                {
                    report.AppendLine($"La fenêtre se trouve sur l'écran {(windowMonitor.IsPrimary ? "principal" : "secondaire")}");

                    // Vérifier si la fenêtre est près du bord de l'écran
                    double rightMargin = windowMonitor.Bounds.Right - (window.Left + window.Width);
                    double leftMargin = window.Left - windowMonitor.Bounds.Left;
                    double topMargin = window.Top - windowMonitor.Bounds.Top;
                    double bottomMargin = windowMonitor.Bounds.Bottom - (window.Top + window.Height);

                    report.AppendLine("Marges par rapport aux bords de l'écran:");
                    report.AppendLine($"  Gauche: {leftMargin:F0} px");
                    report.AppendLine($"  Droite: {rightMargin:F0} px");
                    report.AppendLine($"  Haut: {topMargin:F0} px");
                    report.AppendLine($"  Bas: {bottomMargin:F0} px");

                    // Identifier les problèmes potentiels
                    if (rightMargin < 100)
                    {
                        report.AppendLine("AVERTISSEMENT: La fenêtre est très proche du bord droit de l'écran.");
                        report.AppendLine("  Le preview risque d'être partiellement ou totalement hors de l'écran.");
                    }

                    if (bottomMargin < 100)
                    {
                        report.AppendLine("AVERTISSEMENT: La fenêtre est très proche du bord inférieur de l'écran.");
                        report.AppendLine("  Le preview risque d'être partiellement ou totalement hors de l'écran.");
                    }
                }
                else
                {
                    report.AppendLine("ERREUR: La fenêtre n'est détectée sur aucun écran.");
                    report.AppendLine("  Cela peut indiquer que la fenêtre est entièrement hors des limites visibles.");
                    report.AppendLine("Solution: Repositionnez manuellement la fenêtre pour qu'elle soit visible.");
                }

                // Analyser la stratégie de positionnement
                WindowPreviewManager wpManager = previewManager as WindowPreviewManager;
                if (wpManager != null)
                {
                    // Utilisation de la réflexion pour obtenir des informations sur la stratégie utilisée
                    // Note: Ceci est uniquement à des fins de diagnostic et ne doit pas être utilisé dans le code principal
                    Type wpType = wpManager.GetType();
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                    var strategyField = wpType.GetField("_positionStrategy", flags);

                    if (strategyField != null)
                    {
                        var strategy = strategyField.GetValue(wpManager) as IPositionStrategy;

                        if (strategy != null)
                        {
                            report.AppendLine($"Stratégie de positionnement utilisée: {strategy.GetType().Name}");

                            // Recommandations spécifiques selon le type de stratégie
                            if (strategy is AdjacentPositionStrategy)
                            {
                                report.AppendLine("RECOMMANDATION: La stratégie AdjacentPositionStrategy peut ne pas être optimale");
                                report.AppendLine("  pour les fenêtres positionnées près des bords de l'écran.");
                                report.AppendLine("Solution: Utilisez plutôt SnapPositionStrategy ou Smart pour un meilleur comportement.");
                            }
                            else if (strategy is SnapPositionStrategy)
                            {
                                report.AppendLine("La stratégie SnapPositionStrategy est recommandée pour la plupart des cas.");
                                report.AppendLine("  Elle utilise des zones d'accrochage intelligentes.");
                            }
                            else if (strategy is ConstrainedPositionStrategy)
                            {
                                report.AppendLine("La stratégie ConstrainedPositionStrategy assure que la fenêtre reste visible.");
                                report.AppendLine("  C'est une bonne option pour éviter les problèmes de positionnement.");
                            }

                            // Recommander une stratégie optimale
                            report.AppendLine();
                            report.AppendLine("RECOMMANDATION GÉNÉRALE:");
                            report.AppendLine("  Pour un positionnement optimal, utilisez la méthode EnablePreview avec PositionStrategyType.Smart:");
                            report.AppendLine("  window.EnablePreview(PreviewRendererType.Outline, PositionStrategyType.Smart);");
                        }
                        else
                        {
                            report.AppendLine("ERREUR: Aucune stratégie de positionnement n'est définie.");
                            report.AppendLine("Solution: Définissez une stratégie avec SetPositionStrategy().");
                        }
                    }
                }

                // Recommandations finales
                report.AppendLine();
                report.AppendLine("Recommandations finales:");
                report.AppendLine("1. Assurez-vous que la fenêtre principale n'est pas trop près des bords de l'écran.");
                report.AppendLine("2. Utilisez PositionStrategyType.Smart pour un positionnement intelligent.");
                report.AppendLine("3. Vérifiez que les écrans sont correctement détectés par le système.");
                report.AppendLine("4. Si les problèmes persistent, essayez de redémarrer l'application.");
            }
            catch (Exception ex)
            {
                report.AppendLine($"ERREUR lors du diagnostic: {ex.Message}");
                report.AppendLine($"StackTrace: {ex.StackTrace}");
            }

            return report.ToString();
        }

        /// <summary>
        /// Force la mise à jour de la position du preview en fonction de la position actuelle de la fenêtre
        /// </summary>
        /// <param name="window">Fenêtre dont le preview doit être repositionné</param>
        /// <returns>True si le repositionnement a réussi, sinon False</returns>
        public static bool UpdatePreviewPosition(this Window window)
        {
            try
            {
                // Récupérer le gestionnaire de prévisualisation
                IWindowPreviewManager previewManager = window.GetValue(PreviewManagerProperty) as IWindowPreviewManager;

                // Vérifier si un gestionnaire existe et si une prévisualisation est active
                if (previewManager == null || !previewManager.IsPreviewActive)
                {
                    return false;
                }

                // Mettre à jour la prévisualisation avec les dernières dimensions connues
                // Cela forcera un recalcul de la position
                previewManager.UpdatePreview(previewManager.LastPreviewedSize);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la mise à jour de la position du preview: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Change la stratégie de positionnement utilisée par le gestionnaire de prévisualisation
        /// </summary>
        /// <param name="window">Fenêtre dont la stratégie doit être changée</param>
        /// <param name="strategyType">Nouveau type de stratégie à utiliser</param>
        /// <returns>True si le changement a réussi, sinon False</returns>
        public static bool ChangePositionStrategy(this Window window, PositionStrategyType strategyType)
        {
            try
            {
                // Récupérer le gestionnaire de prévisualisation
                IWindowPreviewManager previewManager = window.GetValue(PreviewManagerProperty) as IWindowPreviewManager;

                // Vérifier si un gestionnaire existe
                if (previewManager == null)
                {
                    return false;
                }

                // Créer une nouvelle stratégie du type demandé
                IPositionStrategy strategy = CreatePositionStrategy(strategyType);

                // Définir la nouvelle stratégie
                previewManager.SetPositionStrategy(strategy);

                // Si une prévisualisation est active, mettre à jour sa position
                if (previewManager.IsPreviewActive)
                {
                    previewManager.UpdatePreview(previewManager.LastPreviewedSize);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du changement de stratégie de positionnement: {ex.Message}");
                return false;
            }
        }

        #endregion
    }

    /// <summary>
    /// Interface définissant le contrat pour un gestionnaire de prévisualisation de fenêtre.
    /// Fournit des méthodes pour contrôler le cycle de vie de la prévisualisation.
    /// </summary>
    public interface IWindowPreviewManager
    {
        /// <summary>
        /// Obtient une valeur indiquant si une prévisualisation est actuellement active
        /// </summary>
        bool IsPreviewActive { get; }

        /// <summary>
        /// Obtient une valeur indiquant si le gestionnaire a été correctement initialisé
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Obtient les dernières dimensions prévisualisées
        /// </summary>
        Size LastPreviewedSize { get; }

        /// <summary>
        /// Événement déclenché lorsqu'une prévisualisation commence
        /// </summary>
        event EventHandler<WindowDimensionEventArgs> PreviewStarted;

        /// <summary>
        /// Événement déclenché lorsqu'une prévisualisation est mise à jour
        /// </summary>
        event EventHandler<WindowDimensionEventArgs> PreviewUpdated;

        /// <summary>
        /// Événement déclenché lorsqu'une prévisualisation se termine
        /// </summary>
        event EventHandler<WindowDimensionEventArgs> PreviewStopped;

        /// <summary>
        /// Événement déclenché lorsque les dimensions prévisualisées sont appliquées
        /// </summary>
        event EventHandler<WindowDimensionEventArgs> PreviewApplied;

        /// <summary>
        /// Initialise le gestionnaire avec la fenêtre cible à prévisualiser
        /// </summary>
        /// <param name="targetWindow">Fenêtre cible dont on veut prévisualiser les dimensions</param>
        void Initialize(Window targetWindow);

        /// <summary>
        /// Démarre une session de prévisualisation avec les dimensions spécifiées
        /// </summary>
        /// <param name="newSize">Dimensions à prévisualiser</param>
        void StartPreview(Size newSize);

        /// <summary>
        /// Met à jour la prévisualisation avec de nouvelles dimensions
        /// </summary>
        /// <param name="newSize">Nouvelles dimensions à prévisualiser</param>
        void UpdatePreview(Size newSize);

        /// <summary>
        /// Arrête la session de prévisualisation en cours
        /// </summary>
        void StopPreview();

        /// <summary>
        /// Applique les dimensions prévisualisées à la fenêtre cible
        /// </summary>
        void ApplyPreviewedDimensions();

        /// <summary>
        /// Définit le fournisseur de dimensions à utiliser
        /// </summary>
        /// <param name="provider">Fournisseur de dimensions à utiliser</param>
        void SetDimensionProvider(IWindowDimensionProvider provider);

        /// <summary>
        /// Définit le renderer à utiliser pour la prévisualisation
        /// </summary>
        /// <param name="renderer">Renderer à utiliser</param>
        void SetPreviewRenderer(IPreviewRenderer renderer);

        /// <summary>
        /// Définit la stratégie de positionnement à utiliser
        /// </summary>
        /// <param name="strategy">Stratégie de positionnement à utiliser</param>
        void SetPositionStrategy(IPositionStrategy strategy);
    }

    /// <summary>
    /// Gestionnaire de préréglages pour les dimensions de fenêtre.
    /// Permet de sauvegarder, charger et gérer des dimensions prédéfinies
    /// avec persistance sur disque au format JSON.
    /// </summary>
    public class PresetManager
    {
        #region Constantes

        /// <summary>
        /// Version actuelle du format de données des presets
        /// </summary>
        private const int PRESET_FORMAT_VERSION = 1;

        /// <summary>
        /// Nom du fichier de sauvegarde des presets personnalisés
        /// </summary>
        private const string PRESET_FILENAME = "window_presets.json";

        #endregion

        #region Champs privés

        // Collection des préréglages disponibles
        private readonly Dictionary<string, Size> _presets = new Dictionary<string, Size>();

        // Collection des préréglages par défaut (jamais modifiée)
        private static readonly Dictionary<string, Size> _defaultPresets = new Dictionary<string, Size>
        {
            { "Petit", new Size(400, 300) },
            { "Moyen", new Size(800, 600) },
            { "Grand", new Size(1024, 768) },
            { "HD", new Size(1280, 720) },
            { "Full HD", new Size(1920, 1080) }
        };

        // Indique si une opération de sauvegarde est en cours
        // Important: ce flag ne doit pas affecter les opérations de lecture pour permettre
        // la synchronisation entre les contrôles et les presets
        private bool _isSaving = false;

        // Chemin complet vers le fichier de sauvegarde des presets
        private string _presetFilePath;

        #endregion

        #region Constructeur et initialisation

        /// <summary>
        /// Initialise une nouvelle instance de PresetManager avec les préréglages par défaut
        /// et charge les préréglages personnalisés depuis le fichier de sauvegarde
        /// </summary>
        public PresetManager()
        {
            // Initialiser le chemin du fichier de sauvegarde
            InitializeFilePath();

            // Initialiser avec les presets par défaut
            InitializeDefaultPresets();

            // Charger les presets personnalisés
            LoadPresetsFromFile();
        }

        /// <summary>
        /// Initialise le chemin du fichier de sauvegarde des presets en utilisant le même
        /// répertoire que les autres paramètres de l'application
        /// </summary>
        private void InitializeFilePath()
        {
            try
            {
                // Utiliser le même répertoire que les autres paramètres de l'application
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string settingsDir = Path.Combine(appDataPath, "HelloWorld");

                // Créer le répertoire s'il n'existe pas
                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }

                // Définir le chemin complet du fichier
                _presetFilePath = Path.Combine(settingsDir, PRESET_FILENAME);

                // Journaliser le chemin pour le débogage
                System.Diagnostics.Debug.WriteLine($"Chemin du fichier de presets: {_presetFilePath}");
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'initialisation du chemin de fichier des presets: {ex.Message}");

                // Utiliser un chemin par défaut en cas d'erreur
                _presetFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "HelloWorld",
                    PRESET_FILENAME);
            }
        }

        /// <summary>
        /// Initialise la collection avec les presets par défaut
        /// </summary>
        private void InitializeDefaultPresets()
        {
            try
            {
                // Vider la collection existante
                _presets.Clear();

                // Copier les préréglages par défaut
                foreach (var preset in _defaultPresets)
                {
                    _presets.Add(preset.Key, preset.Value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'initialisation des presets par défaut: {ex.Message}");
            }
        }

        #endregion

        #region Persistance des presets

        /// <summary>
        /// Classe pour stocker les données des presets personnalisés
        /// </summary>
        private class UserPresets
        {
            /// <summary>
            /// Collection des presets personnalisés (nom et dimensions)
            /// </summary>
            public Dictionary<string, Size> Presets { get; set; } = new Dictionary<string, Size>();

            /// <summary>
            /// Indique les presets par défaut qui ont été supprimés par l'utilisateur
            /// </summary>
            public List<string> RemovedDefaultPresets { get; set; } = new List<string>();
        }

        /// <summary>
        /// Charge les presets personnalisés depuis le fichier
        /// </summary>
        /// <returns>True si le chargement a réussi, sinon False</returns>
        private bool LoadPresetsFromFile()
        {
            try
            {
                // Vérifier si le fichier existe
                if (!File.Exists(_presetFilePath))
                {
                    // Pas de fichier, on garde uniquement les presets par défaut
                    return false;
                }

                // Charger les presets personnalisés
                bool success;
                UserPresets userPresets = JsonSettingsManager.LoadFromFile<UserPresets>(
                    _presetFilePath,
                    () => new UserPresets(),
                    PRESET_FORMAT_VERSION,
                    out success);

                if (!success || userPresets == null)
                {
                    // Erreur de chargement, on garde uniquement les presets par défaut
                    return false;
                }

                // Supprimer les presets par défaut qui ont été explicitement supprimés par l'utilisateur
                foreach (string removedPreset in userPresets.RemovedDefaultPresets)
                {
                    _presets.Remove(removedPreset);
                }

                // Ajouter/remplacer avec les presets personnalisés
                foreach (var preset in userPresets.Presets)
                {
                    _presets[preset.Key] = preset.Value;
                }

                return true;
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des presets: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sauvegarde les presets personnalisés dans le fichier
        /// </summary>
        /// <returns>True si la sauvegarde a réussi, sinon False</returns>
        private bool SavePresetsToFile()
        {
            // Éviter les sauvegardes récursives
            if (_isSaving)
                return false;

            _isSaving = true;

            try
            {
                // Créer un nouvel objet pour stocker les données personnalisées
                UserPresets userPresets = new UserPresets();

                // Identifier les presets par défaut qui ont été supprimés
                foreach (var defaultPreset in _defaultPresets)
                {
                    if (!_presets.ContainsKey(defaultPreset.Key))
                    {
                        userPresets.RemovedDefaultPresets.Add(defaultPreset.Key);
                    }
                }

                // Identifier les presets personnalisés (différents des presets par défaut)
                foreach (var preset in _presets)
                {
                    // Vérifier si ce preset est personnalisé
                    bool isCustom = !_defaultPresets.ContainsKey(preset.Key);

                    // Vérifier si ce preset est par défaut mais a été modifié
                    bool isModifiedDefault = _defaultPresets.ContainsKey(preset.Key) &&
                        !SizesAreEqual(_defaultPresets[preset.Key], preset.Value);

                    // Si c'est un preset personnalisé ou un preset par défaut modifié, l'ajouter
                    if (isCustom || isModifiedDefault)
                    {
                        userPresets.Presets[preset.Key] = preset.Value;
                    }
                }

                // Vérifier s'il y a des données à sauvegarder
                if (userPresets.Presets.Count == 0 && userPresets.RemovedDefaultPresets.Count == 0)
                {
                    // Rien à sauvegarder, on peut supprimer le fichier s'il existe
                    if (File.Exists(_presetFilePath))
                    {
                        File.Delete(_presetFilePath);
                    }
                    return true;
                }

                // Sauvegarder les presets
                return JsonSettingsManager.SaveToFile(userPresets, _presetFilePath, PRESET_FORMAT_VERSION);
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la sauvegarde des presets: {ex.Message}");
                return false;
            }
            finally
            {
                _isSaving = false;
            }
        }

        /// <summary>
        /// Compare deux objets Size pour déterminer s'ils sont égaux
        /// </summary>
        /// <param name="size1">Premier objet Size</param>
        /// <param name="size2">Second objet Size</param>
        /// <returns>True si les deux objets sont égaux (à 1 pixel près), sinon False</returns>
        private bool SizesAreEqual(Size size1, Size size2)
        {
            // Utiliser une tolérance de 1 pixel pour la comparaison
            return Math.Abs(size1.Width - size2.Width) < 1 &&
                   Math.Abs(size1.Height - size2.Height) < 1;
        }

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Ajoute un préréglage
        /// </summary>
        /// <param name="name">Nom du préréglage</param>
        /// <param name="size">Dimensions du préréglage</param>
        /// <returns>True si le préréglage a été ajouté, False s'il existait déjà</returns>
        public bool AddPreset(string name, Size size)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Le nom du préréglage ne peut pas être vide", nameof(name));

            if (_presets.ContainsKey(name))
                return false;

            _presets.Add(name, size);

            // Sauvegarder les presets
            SavePresetsToFile();

            return true;
        }

        /// <summary>
        /// Ajoute ou met à jour un préréglage
        /// </summary>
        /// <param name="name">Nom du préréglage</param>
        /// <param name="size">Dimensions du préréglage</param>
        public void SetPreset(string name, Size size)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Le nom du préréglage ne peut pas être vide", nameof(name));

            _presets[name] = size;

            // Sauvegarder les presets
            SavePresetsToFile();
        }

        /// <summary>
        /// Supprime un préréglage
        /// </summary>
        /// <param name="name">Nom du préréglage à supprimer</param>
        /// <returns>True si le préréglage a été supprimé, False s'il n'existait pas</returns>
        public bool RemovePreset(string name)
        {
            bool result = _presets.Remove(name);

            if (result)
            {
                // Sauvegarder les presets uniquement si un preset a été supprimé
                SavePresetsToFile();
            }

            return result;
        }

        /// <summary>
        /// Obtient les dimensions d'un préréglage
        /// </summary>
        /// <param name="name">Nom du préréglage</param>
        /// <returns>Dimensions du préréglage ou Size.Empty si le préréglage n'existe pas</returns>
        public Size GetPreset(string name)
        {
            if (_presets.TryGetValue(name, out Size size))
                return size;

            return Size.Empty;
        }

        /// <summary>
        /// Vérifie si un préréglage existe
        /// </summary>
        /// <param name="name">Nom du préréglage</param>
        /// <returns>True si le préréglage existe, sinon False</returns>
        public bool PresetExists(string name)
        {
            return _presets.ContainsKey(name);
        }

        /// <summary>
        /// Obtient tous les noms de préréglages disponibles
        /// </summary>
        /// <returns>Collection des noms de préréglages</returns>
        public IEnumerable<string> GetPresetNames()
        {
            return _presets.Keys.ToList();
        }

        /// <summary>
        /// Obtient tous les préréglages disponibles
        /// </summary>
        /// <returns>Collection des préréglages (nom et dimensions)</returns>
        public IEnumerable<KeyValuePair<string, Size>> GetAllPresets()
        {
            return _presets;
        }

        /// <summary>
        /// Réinitialise les préréglages aux valeurs par défaut
        /// </summary>
        public void ResetToDefaults()
        {
            _presets.Clear();

            // Copier les préréglages par défaut
            foreach (var preset in _defaultPresets)
            {
                _presets.Add(preset.Key, preset.Value);
            }

            // Sauvegarder les presets (ou plutôt, supprimer le fichier de sauvegarde)
            SavePresetsToFile();
        }

        /// <summary>
        /// Applique un préréglage à une fenêtre
        /// </summary>
        /// <param name="window">Fenêtre à redimensionner</param>
        /// <param name="presetName">Nom du préréglage à appliquer</param>
        /// <returns>True si le préréglage a été appliqué, False s'il n'existait pas</returns>
        public bool ApplyPresetToWindow(Window window, string presetName)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            Size presetSize = GetPreset(presetName);
            if (presetSize == Size.Empty)
                return false;

            window.Width = presetSize.Width;
            window.Height = presetSize.Height;
            return true;
        }

        /// <summary>
        /// Prévisualise un préréglage sur une fenêtre
        /// </summary>
        /// <param name="window">Fenêtre à prévisualiser</param>
        /// <param name="presetName">Nom du préréglage à prévisualiser</param>
        /// <returns>True si le préréglage a été prévisualisé, False s'il n'existait pas</returns>
        public bool PreviewPresetOnWindow(Window window, string presetName)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            Size presetSize = GetPreset(presetName);
            if (presetSize == Size.Empty)
                return false;

            // Récupérer le gestionnaire de prévisualisation
            IWindowPreviewManager previewManager = window.GetPreviewManager();
            if (previewManager == null)
            {
                // Créer un nouveau gestionnaire si aucun n'existe
                previewManager = window.EnablePreview();
            }

            // Démarrer ou mettre à jour la prévisualisation
            if (previewManager.IsPreviewActive)
            {
                previewManager.UpdatePreview(presetSize);
            }
            else
            {
                previewManager.StartPreview(presetSize);
            }

            return true;
        }

        /// <summary>
        /// Exporte tous les presets vers un fichier
        /// </summary>
        /// <param name="filePath">Chemin du fichier d'exportation</param>
        /// <returns>True si l'exportation a réussi, sinon False</returns>
        public bool ExportPresetsToFile(string filePath)
        {
            try
            {
                // Créer une copie des presets actuels
                var presetsToExport = new Dictionary<string, Size>(_presets);

                // Sauvegarder dans un fichier spécifié par l'utilisateur
                return JsonSettingsManager.SaveToFile(
                    presetsToExport,
                    filePath,
                    PRESET_FORMAT_VERSION);
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'exportation des presets: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Importe des presets depuis un fichier
        /// </summary>
        /// <param name="filePath">Chemin du fichier d'importation</param>
        /// <param name="mergeWithExisting">Si True, fusionne avec les presets existants, sinon remplace</param>
        /// <returns>True si l'importation a réussi, sinon False</returns>
        public bool ImportPresetsFromFile(string filePath, bool mergeWithExisting = true)
        {
            try
            {
                // Vérifier que le fichier existe
                if (!File.Exists(filePath))
                    return false;

                // Charger les presets depuis le fichier spécifié
                bool success;
                var importedPresets = JsonSettingsManager.LoadFromFile<Dictionary<string, Size>>(
                    filePath,
                    () => new Dictionary<string, Size>(),
                    PRESET_FORMAT_VERSION,
                    out success);

                if (!success || importedPresets == null || importedPresets.Count == 0)
                    return false;

                // Si on ne fusionne pas, effacer les presets actuels sauf les presets par défaut
                if (!mergeWithExisting)
                {
                    var defaultPresetKeys = _defaultPresets.Keys.ToList();
                    var currentPresetKeys = _presets.Keys.ToList();

                    foreach (var key in currentPresetKeys)
                    {
                        if (!defaultPresetKeys.Contains(key))
                        {
                            _presets.Remove(key);
                        }
                    }
                }

                // Ajouter ou remplacer les presets importés
                foreach (var preset in importedPresets)
                {
                    _presets[preset.Key] = preset.Value;
                }

                // Sauvegarder les nouveaux presets
                return SavePresetsToFile();
            }
            catch (Exception ex)
            {
                // Journaliser l'erreur
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'importation des presets: {ex.Message}");
                return false;
            }
        }

        #endregion
    }

    /// <summary>
    /// Méthodes d'extension pour la classe PresetManager
    /// </summary>
    public static class PresetManagerExtensions
    {
        /// <summary>
        /// Connecte un ComboBox aux préréglages disponibles
        /// </summary>
        /// <param name="presetManager">Gestionnaire de préréglages</param>
        /// <param name="comboBox">ComboBox à connecter</param>
        /// <returns>Le gestionnaire de préréglages (pour le chaînage)</returns>
        public static PresetManager ConnectToComboBox(
            this PresetManager presetManager,
            ComboBox comboBox)
        {
            if (comboBox == null)
                throw new ArgumentNullException(nameof(comboBox));

            // Effacer les éléments existants
            comboBox.Items.Clear();

            // Ajouter tous les préréglages disponibles
            foreach (string presetName in presetManager.GetPresetNames())
            {
                comboBox.Items.Add(presetName);
            }

            return presetManager;
        }

        /// <summary>
        /// Connecte un ComboBox et une fenêtre aux préréglages disponibles
        /// </summary>
        /// <param name="presetManager">Gestionnaire de préréglages</param>
        /// <param name="comboBox">ComboBox à connecter</param>
        /// <param name="window">Fenêtre à prévisualiser/redimensionner</param>
        /// <param name="previewCheckBox">CheckBox pour activer/désactiver la prévisualisation (optionnel)</param>
        /// <returns>Le gestionnaire de préréglages (pour le chaînage)</returns>
        public static PresetManager ConnectToWindow(
            this PresetManager presetManager,
            ComboBox comboBox,
            Window window,
            CheckBox previewCheckBox = null)
        {
            if (comboBox == null)
                throw new ArgumentNullException(nameof(comboBox));

            if (window == null)
                throw new ArgumentNullException(nameof(window));

            // Connecter les préréglages au ComboBox
            presetManager.ConnectToComboBox(comboBox);

            // S'abonner à l'événement de sélection
            comboBox.SelectionChanged += (sender, e) =>
            {
                string selectedPreset = comboBox.SelectedItem as string;
                if (string.IsNullOrEmpty(selectedPreset))
                    return;

                bool usePreview = previewCheckBox?.IsChecked ?? false;

                if (usePreview)
                {
                    // Prévisualiser le préréglage
                    presetManager.PreviewPresetOnWindow(window, selectedPreset);
                }
                else
                {
                    // Appliquer directement le préréglage
                    presetManager.ApplyPresetToWindow(window, selectedPreset);
                }
            };

            // S'abonner à l'événement de la CheckBox si fournie
            if (previewCheckBox != null)
            {
                previewCheckBox.Checked += (sender, e) =>
                {
                    string selectedPreset = comboBox.SelectedItem as string;
                    if (!string.IsNullOrEmpty(selectedPreset))
                    {
                        // Prévisualiser le préréglage sélectionné
                        presetManager.PreviewPresetOnWindow(window, selectedPreset);
                    }
                };

                previewCheckBox.Unchecked += (sender, e) =>
                {
                    // Arrêter la prévisualisation active
                    IWindowPreviewManager previewManager = window.GetPreviewManager();
                    if (previewManager != null && previewManager.IsPreviewActive)
                    {
                        previewManager.StopPreview();
                    }
                };
            }

            return presetManager;
        }

        /// <summary>
        /// Crée un fournisseur de dimensions basé sur ce gestionnaire de préréglages
        /// </summary>
        /// <param name="presetManager">Gestionnaire de préréglages</param>
        /// <param name="defaultPresetName">Nom du préréglage par défaut</param>
        /// <returns>Fournisseur de dimensions</returns>
        public static IWindowDimensionProvider CreateDimensionProvider(
            this PresetManager presetManager,
            string defaultPresetName = null)
        {
            // Obtenir les dimensions du préréglage par défaut
            Size defaultSize = Size.Empty;
            if (!string.IsNullOrEmpty(defaultPresetName))
            {
                defaultSize = presetManager.GetPreset(defaultPresetName);
            }

            // Si aucun préréglage par défaut n'est spécifié ou n'existe, utiliser le premier préréglage
            if (defaultSize == Size.Empty)
            {
                var presets = presetManager.GetAllPresets();
                var enumerator = presets.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    defaultSize = enumerator.Current.Value;
                }
                else
                {
                    // Si aucun préréglage n'est disponible, utiliser une taille par défaut
                    defaultSize = new Size(800, 600);
                }
            }

            // Créer un fournisseur de dimensions fixes
            return new FixedDimensionProvider(defaultSize);
        }
    }

    /// <summary>
    /// Utilitaire pour gérer les informations d'écran.
    /// Fournit des méthodes pour obtenir des informations sur les écrans disponibles.
    /// </summary>
    public static class ScreenUtility
    {
        /// <summary>
        /// Informations sur un moniteur
        /// </summary>
        public class MonitorInfo
        {
            /// <summary>
            /// Représente les limites d'un rectangle
            /// </summary>
            public struct RECT
            {
                /// <summary>
                /// Coordonnée X du coin supérieur gauche
                /// </summary>
                public int Left;

                /// <summary>
                /// Coordonnée Y du coin supérieur gauche
                /// </summary>
                public int Top;

                /// <summary>
                /// Coordonnée X du coin inférieur droit
                /// </summary>
                public int Right;

                /// <summary>
                /// Coordonnée Y du coin inférieur droit
                /// </summary>
                public int Bottom;
            }

            /// <summary>
            /// Limites de l'écran (espace total)
            /// </summary>
            public RECT Bounds;

            /// <summary>
            /// Limites de l'espace de travail (espace sans la barre des tâches)
            /// </summary>
            public RECT WorkArea;

            /// <summary>
            /// Indique si c'est l'écran principal
            /// </summary>
            public bool IsPrimary;

            /// <summary>
            /// Nom de l'appareil
            /// </summary>
            public string DeviceName;

            /// <summary>
            /// Obtient la largeur de l'écran
            /// </summary>
            public int Width => Bounds.Right - Bounds.Left;

            /// <summary>
            /// Obtient la hauteur de l'écran
            /// </summary>
            public int Height => Bounds.Bottom - Bounds.Top;

            /// <summary>
            /// Obtient la largeur de l'espace de travail
            /// </summary>
            public int WorkAreaWidth => WorkArea.Right - WorkArea.Left;

            /// <summary>
            /// Obtient la hauteur de l'espace de travail
            /// </summary>
            public int WorkAreaHeight => WorkArea.Bottom - WorkArea.Top;
        }

        /// <summary>
        /// Obtient les informations sur tous les moniteurs disponibles
        /// </summary>
        public static System.Collections.Generic.IEnumerable<MonitorInfo> Monitors
        {
            get
            {
                var monitors = new System.Collections.Generic.List<MonitorInfo>();

                // Récupérer les informations sur tous les écrans
                foreach (var screen in System.Windows.Forms.Screen.AllScreens)
                {
                    var monitorInfo = new MonitorInfo
                    {
                        Bounds = new MonitorInfo.RECT
                        {
                            Left = screen.Bounds.Left,
                            Top = screen.Bounds.Top,
                            Right = screen.Bounds.Right,
                            Bottom = screen.Bounds.Bottom
                        },
                        WorkArea = new MonitorInfo.RECT
                        {
                            Left = screen.WorkingArea.Left,
                            Top = screen.WorkingArea.Top,
                            Right = screen.WorkingArea.Right,
                            Bottom = screen.WorkingArea.Bottom
                        },
                        IsPrimary = screen.Primary,
                        DeviceName = screen.DeviceName
                    };

                    monitors.Add(monitorInfo);
                }

                return monitors;
            }
        }

        /// <summary>
        /// Obtient l'écran principal
        /// </summary>
        public static MonitorInfo PrimaryMonitor
        {
            get
            {
                foreach (var monitor in Monitors)
                {
                    if (monitor.IsPrimary)
                        return monitor;
                }

                // Si aucun écran n'est marqué comme principal, retourner le premier écran
                var enumerator = Monitors.GetEnumerator();
                if (enumerator.MoveNext())
                    return enumerator.Current;

                return null;
            }
        }

        /// <summary>
        /// Trouve l'écran qui contient un point
        /// </summary>
        /// <param name="point">Point à tester</param>
        /// <returns>Écran contenant le point, ou null si aucun écran ne contient le point</returns>
        public static MonitorInfo FindMonitorContainingPoint(Point point)
        {
            foreach (var monitor in Monitors)
            {
                if (point.X >= monitor.Bounds.Left && point.X < monitor.Bounds.Right &&
                    point.Y >= monitor.Bounds.Top && point.Y < monitor.Bounds.Bottom)
                {
                    return monitor;
                }
            }

            return null;
        }

        /// <summary>
        /// Trouve l'écran qui contient une fenêtre
        /// </summary>
        /// <param name="window">Fenêtre à tester</param>
        /// <returns>Écran contenant la fenêtre, ou null si aucun écran ne contient la fenêtre</returns>
        public static MonitorInfo FindMonitorContainingWindow(Window window)
        {
            if (window == null)
                return null;

            // Obtenir le centre de la fenêtre
            Point center = new Point(
                window.Left + window.Width / 2,
                window.Top + window.Height / 2);

            return FindMonitorContainingPoint(center);
        }

        /// <summary>
        /// Vérifie si un rectangle est entièrement visible sur un écran
        /// </summary>
        /// <param name="rect">Rectangle à tester</param>
        /// <returns>True si le rectangle est entièrement visible, sinon False</returns>
        public static bool IsRectangleFullyVisible(Rect rect)
        {
            foreach (var monitor in Monitors)
            {
                Rect monitorRect = new Rect(
                    monitor.Bounds.Left,
                    monitor.Bounds.Top,
                    monitor.Width,
                    monitor.Height);

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
        public static bool IsRectanglePartiallyVisible(Rect rect, double minVisiblePercent = 0.5)
        {
            double rectArea = rect.Width * rect.Height;
            if (rectArea <= 0)
                return false;

            foreach (var monitor in Monitors)
            {
                Rect monitorRect = new Rect(
                    monitor.Bounds.Left,
                    monitor.Bounds.Top,
                    monitor.Width,
                    monitor.Height);

                if (rect.IntersectsWith(monitorRect))
                {
                    Rect intersection = Rect.Intersect(rect, monitorRect);
                    double visibleArea = intersection.Width * intersection.Height;

                    if (visibleArea >= rectArea * minVisiblePercent)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}