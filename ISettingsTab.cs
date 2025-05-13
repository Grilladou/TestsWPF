using System;

namespace HelloWorld
{
    /// <summary>
    /// Interface commune pour tous les onglets de paramètres.
    /// Cette interface standardise la façon dont les différents onglets de paramètres
    /// communiquent avec le panneau latéral principal (SidePanel).
    /// </summary>
    public interface ISettingsTab
    {
        /// <summary>
        /// Événement déclenché lorsque les paramètres sont appliqués avec succès
        /// </summary>
        event EventHandler SettingsApplied;

        /// <summary>
        /// Événement déclenché lorsque la réinitialisation des paramètres est demandée
        /// </summary>
        event EventHandler ResetRequested;

        /// <summary>
        /// Événement déclenché lorsque les paramètres sont validés
        /// </summary>
        event EventHandler<SettingsValidationEventArgs> SettingsValidated;

        /// <summary>
        /// Charge les paramètres actuels dans l'interface utilisateur
        /// </summary>
        void LoadCurrentSettings();

        /// <summary>
        /// Valide les paramètres entrés par l'utilisateur
        /// </summary>
        /// <param name="errorMessage">Message d'erreur si la validation échoue</param>
        /// <returns>True si tous les paramètres sont valides, sinon False</returns>
        bool ValidateSettings(out string errorMessage);

        /// <summary>
        /// Applique les paramètres modifiés
        /// </summary>
        /// <returns>True si l'application des paramètres a réussi, sinon False</returns>
        bool ApplySettings();

        /// <summary>
        /// Réinitialise les paramètres à leurs valeurs par défaut
        /// </summary>
        void ResetSettings();

        /// <summary>
        /// Effectue un test de la fonctionnalité principale associée à l'onglet
        /// </summary>
        /// <remarks>Cette méthode est optionnelle et peut ne rien faire dans certaines implémentations</remarks>
        void TestSettings();
    }

    /// <summary>
    /// Arguments d'événement pour la validation des paramètres
    /// </summary>
    public class SettingsValidationEventArgs : EventArgs
    {
        /// <summary>
        /// Indique si la validation a réussi
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Message d'erreur en cas d'échec de validation
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Crée une nouvelle instance de SettingsValidationEventArgs
        /// </summary>
        /// <param name="isValid">Indique si la validation a réussi</param>
        /// <param name="errorMessage">Message d'erreur en cas d'échec</param>
        public SettingsValidationEventArgs(bool isValid, string errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
    }
}