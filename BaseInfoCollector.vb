Imports System.Threading.Tasks

''' <summary>
''' Classe de base pour tous les utilitaires de collecte d'informations.
''' Fournit des fonctionnalités communes comme les événements de progression
''' et la gestion de l'annulation.
''' </summary>
Public MustInherit Class BaseInfoCollector
    ' ========== SECTION DE DÉFINITION DES ÉVÉNEMENTS ET CONSTANTES ==========

    ''' <summary>
    ''' Délégué pour les événements de progression qui permet de communiquer l'état d'avancement
    ''' avec un message, l'étape courante et le nombre total d'étapes.
    ''' </summary>
    Public Delegate Sub ProgressUpdateEventHandler(message As String, current As Integer, total As Integer)

    ''' <summary>
    ''' Événement déclenché pour notifier de la progression du processus de collecte.
    ''' </summary>
    Public Shared Event ProgressUpdate As ProgressUpdateEventHandler

    ''' <summary>
    ''' Détermine si une popup de résultat est affichée pendant la collecte.
    ''' </summary>
    Public Shared ShowResultPopup As Boolean = True

    ''' <summary>
    ''' Délai en secondes entre chaque étape de collecte d'informations.
    ''' </summary>
    Public Shared StepDelayInSeconds As Integer = 1

    ''' <summary>
    ''' Indicateur pour savoir si une demande d'annulation a été émise.
    ''' </summary>
    Public Shared CancellationRequested As Boolean = False

    ' ========== MÉTHODES PARTAGÉES DE GESTION ==========

    ''' <summary>
    ''' Demande l'annulation du processus de collecte en cours.
    ''' </summary>
    Public Shared Sub RequestCancellation()
        CancellationRequested = True
    End Sub

    ''' <summary>
    ''' Réinitialise l'état d'annulation.
    ''' </summary>
    Public Shared Sub ResetCancellation()
        CancellationRequested = False
    End Sub

    ''' <summary>
    ''' Notifie un changement de progression.
    ''' </summary>
    Protected Shared Sub NotifyProgress(message As String, current As Integer, total As Integer)
        RaiseEvent ProgressUpdate(message, current, total)
    End Sub

    ''' <summary>
    ''' Applique un délai entre les étapes si configuré et vérifie si l'annulation a été demandée.
    ''' </summary>
    ''' <returns>True si la collecte peut continuer, False si elle doit être interrompue</returns>
    Protected Shared Function ApplyDelayAndCheckCancellation() As Boolean
        ' Appliquer le délai configuré
        If StepDelayInSeconds > 0 Then
            System.Threading.Thread.Sleep(StepDelayInSeconds * 1000)
        End If

        ' Retourner l'inverse de CancellationRequested pour indiquer si on peut continuer
        Return Not CancellationRequested
    End Function

    ''' <summary>
    ''' Formate une valeur en octets en une unité plus lisible (Ko, Mo, Go, To).
    ''' </summary>
    Protected Shared Function FormatBytes(bytes As ULong) As String
        ' Définir la taille de l'unité (1024 pour les unités binaires)
        Const unit As ULong = 1024

        ' Si la valeur est inférieure à 1 Ko, afficher en octets
        If bytes < unit Then
            Return $"{bytes} octets"
        End If

        ' Calculer l'exposant pour déterminer l'unité (Ko, Mo, Go, To...)
        Dim exp = CInt(Math.Log(bytes, unit))

        ' Convertir la valeur dans l'unité correspondante
        Dim size = bytes / Math.Pow(unit, exp)

        ' Déterminer le suffixe de l'unité (K pour Ko, M pour Mo, etc.)
        ' KMGTPE correspond à Kilo, Mega, Giga, Tera, Peta, Exa
        Dim suffix As String = "KMGTPE".Substring(exp - 1, 1)

        ' Formater la valeur avec 2 décimales et ajouter le suffixe
        Return $"{size:N2} {suffix}o"
    End Function

    ''' <summary>
    ''' Récupère une propriété d'un objet WMI de manière sécurisée.
    ''' </summary>
    Protected Shared Function SafeGetProperty(obj As Management.ManagementObject, propertyName As String) As String
        Try
            ' Vérifier si la propriété existe et n'est pas null
            If obj(propertyName) IsNot Nothing Then
                Return obj(propertyName).ToString()
            End If
        Catch
            ' Ignorer les erreurs d'accès aux propriétés
        End Try

        ' Valeur par défaut si la propriété n'existe pas ou est inaccessible
        Return "Non disponible"
    End Function
End Class