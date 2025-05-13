using System;
using HelloWorld.Preview;

namespace HelloWorld
{
    /// <summary>
    /// Extension partielle de la classe InterfaceSettingsModule pour gérer les fonctionnalités de prévisualisation.
    /// Cette partie contient toutes les propriétés et méthodes liées à la gestion des options de prévisualisation
    /// pour les dimensions de la fenêtre principale de l'application.
    /// </summary>
    public partial class InterfaceSettingsModule
    {
        #region Propriétés pour les paramètres de prévisualisation

        private DimensionIndicatorType _indicatorsType = DimensionIndicatorType.PixelsOnly;  // Modifié
        /// <summary>
        /// Type d'indicateurs affichés lors de la prévisualisation
        /// </summary>
        public DimensionIndicatorType IndicatorsType  // Modifié
        {
            get { return _indicatorsType; }
            set
            {
                if (_indicatorsType != value)
                {
                    _indicatorsType = value;
                    OnPropertyChanged(nameof(IndicatorsType));
                    NotifySettingsChanged();
                }
            }
        }

        private PreviewModeType _previewMode = PreviewModeType.Thumbnail;
        /// <summary>
        /// Mode d'affichage pour la prévisualisation
        /// </summary>
        public PreviewModeType PreviewMode
        {
            get { return _previewMode; }
            set
            {
                if (_previewMode != value)
                {
                    _previewMode = value;
                    OnPropertyChanged(nameof(PreviewMode));
                    NotifySettingsChanged();
                }
            }
        }

        private double _temporaryPreviewDuration = 5.0;
        /// <summary>
        /// Durée de la prévisualisation temporaire en secondes
        /// </summary>
        public double TemporaryPreviewDuration
        {
            get { return _temporaryPreviewDuration; }
            set
            {
                if (_temporaryPreviewDuration != value)
                {
                    _temporaryPreviewDuration = value;
                    OnPropertyChanged(nameof(TemporaryPreviewDuration));
                    NotifySettingsChanged();
                }
            }
        }

        private bool _showSnapZones = false;
        /// <summary>
        /// Indique si les zones d'accrochage Windows sont affichées pendant la prévisualisation
        /// </summary>
        public bool ShowSnapZones
        {
            get { return _showSnapZones; }
            set
            {
                if (_showSnapZones != value)
                {
                    _showSnapZones = value;
                    OnPropertyChanged(nameof(ShowSnapZones));
                    NotifySettingsChanged();
                }
            }
        }

        private PreviewRendererType _previewRendererType = PreviewRendererType.Simulated;
        /// <summary>
        /// Type de renderer utilisé pour la prévisualisation
        /// </summary>
        public PreviewRendererType PreviewRendererType
        {
            get { return _previewRendererType; }
            set
            {
                if (_previewRendererType != value)
                {
                    _previewRendererType = value;
                    OnPropertyChanged(nameof(PreviewRendererType));
                    NotifySettingsChanged();
                }
            }
        }

        private PositionStrategyType _positionStrategyType = PositionStrategyType.Snap;
        /// <summary>
        /// Type de stratégie de positionnement utilisé pour la prévisualisation
        /// </summary>
        public PositionStrategyType PositionStrategyType
        {
            get { return _positionStrategyType; }
            set
            {
                if (_positionStrategyType != value)
                {
                    _positionStrategyType = value;
                    OnPropertyChanged(nameof(PositionStrategyType));
                    NotifySettingsChanged();
                }
            }
        }

        #endregion

        #region Méthodes de gestion des paramètres de prévisualisation

        /// <summary>
        /// Met à jour les paramètres de prévisualisation à partir d'un objet de configuration
        /// </summary>
        /// <param name="data">Données de configuration contenant les informations de prévisualisation</param>
        private void UpdatePreviewDataFromSettings(InterfaceSettingsData data)
        {
            // Vérification de la validité des données d'entrée
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Les données de configuration ne peuvent pas être nulles.");
            }

            // Mettre à jour les propriétés liées à la prévisualisation
            _indicatorsType = data.IndicatorsType;
            _previewMode = data.PreviewMode;
            _temporaryPreviewDuration = data.TemporaryPreviewDuration;
            _showSnapZones = data.ShowSnapZones;
            _previewRendererType = data.PreviewRendererType;
            _positionStrategyType = data.PositionStrategyType;

            // Notifier du changement
            OnPropertyChanged(nameof(IndicatorsType));
            OnPropertyChanged(nameof(PreviewMode));
            OnPropertyChanged(nameof(TemporaryPreviewDuration));
            OnPropertyChanged(nameof(ShowSnapZones));
            OnPropertyChanged(nameof(PreviewRendererType));
            OnPropertyChanged(nameof(PositionStrategyType));
        }

        /// <summary>
        /// Ajoute les paramètres de prévisualisation actuels à un objet de configuration
        /// </summary>
        /// <param name="data">Objet de configuration à compléter avec les paramètres de prévisualisation</param>
        private void AddPreviewDataToSettings(InterfaceSettingsData data)
        {
            // Vérification de la validité des données d'entrée
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Les données de configuration ne peuvent pas être nulles.");
            }

            // Ajouter les propriétés de configuration de prévisualisation
            data.IndicatorsType = _indicatorsType;
            data.PreviewMode = _previewMode;
            data.TemporaryPreviewDuration = _temporaryPreviewDuration;
            data.ShowSnapZones = _showSnapZones;
            data.PreviewRendererType = _previewRendererType;
            data.PositionStrategyType = _positionStrategyType;
        }

        /// <summary>
        /// Réinitialise les paramètres de prévisualisation à leurs valeurs par défaut
        /// </summary>
        public void ResetPreviewSettings()
        {
            // Réinitialiser chaque propriété à sa valeur par défaut
            IndicatorsType = DimensionIndicatorType.PixelsOnly;  // Modifié
            PreviewMode = PreviewModeType.Thumbnail;
            TemporaryPreviewDuration = 5.0;
            ShowSnapZones = false;
            PreviewRendererType = PreviewRendererType.Simulated;
            PositionStrategyType = PositionStrategyType.Snap;

            // Notification effectuée dans les setters de chaque propriété
        }

        /// <summary>
        /// Applique les paramètres de prévisualisation à un adaptateur de prévisualisation
        /// </summary>
        /// <param name="previewAdapter">Adaptateur de prévisualisation à configurer</param>
        public void ApplyPreviewSettingsToAdapter(InterfaceSettingsPreviewAdapter previewAdapter)
        {
            if (previewAdapter == null)
            {
                throw new ArgumentNullException(nameof(previewAdapter), "L'adaptateur de prévisualisation ne peut pas être nul.");
            }

            // Configurer l'adaptateur avec les paramètres actuels
            previewAdapter.ChangeRenderer(PreviewRendererType);
            previewAdapter.ChangePositionStrategy(PositionStrategyType);

            // Note: D'autres propriétés comme IndicatorsType, PreviewMode, etc.
            // peuvent nécessiter des méthodes supplémentaires dans l'adaptateur
        }

        #endregion
    }
}