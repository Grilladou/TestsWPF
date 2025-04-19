Imports System.Management
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
    ''' Détermine si la fenêtre de chargement (LoadingWindow) est affichée pendant la collecte.
    ''' Cette propriété sera chargée depuis un fichier de configuration JSON dans une future implémentation.
    ''' </summary>
    Public Shared ShowLoadingWindow As Boolean = True

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
    ''' Notifie un changement de progression de manière thread-safe.
    ''' </summary>
    Protected Shared Sub NotifyProgress(message As String, current As Integer, total As Integer)
        ' Créer une copie locale du gestionnaire d'événements pour éviter la modification de la référence 
        ' entre la vérification et l'invocation (thread-safe)
        Dim handler = ProgressUpdateEvent

        ' Vérifier si un gestionnaire est abonné
        If handler IsNot Nothing Then
            ' Déclencher l'événement
            handler(message, current, total)
        End If
    End Sub

    ''' <summary>
    ''' Applique un délai entre les étapes si configuré et vérifie si l'annulation a été demandée.
    ''' </summary>
    ''' <returns>True si la collecte peut continuer, False si elle doit être interrompue</returns>
    ' Méthode synchrone qui appelle la méthode asynchrone
    Protected Shared Function ApplyDelayAndCheckCancellation() As Boolean
        ' Appeler la méthode asynchrone de manière synchrone
        Return ApplyDelayAndCheckCancellationAsync().GetAwaiter().GetResult()
    End Function

    ' Méthode asynchrone pour les futures utilisations
    Protected Shared Async Function ApplyDelayAndCheckCancellationAsync() As Task(Of Boolean)
        ' Appliquer le délai configuré de manière asynchrone
        If StepDelayInSeconds > 0 Then
            Await Task.Delay(StepDelayInSeconds * 1000)
        End If

        ' Retourner l'inverse de CancellationRequested pour indiquer si on peut continuer
        Return Not CancellationRequested
    End Function

    ''' <summary>
    ''' Exécute une requête WMI de manière asynchrone.
    ''' </summary>
    ''' <param name="query">La requête WMI à exécuter</param>
    ''' <returns>Une collection de ManagementObject résultant de la requête ou null en cas d'erreur</returns>
    Protected Shared Async Function QueryWmiAsync(query As String) As Task(Of ManagementObjectCollection)
        ' Vérifier si une annulation a été demandée avant même de commencer
        If CancellationRequested Then
            Return Nothing
        End If

        ' Exécuter la requête WMI sur un thread séparé
        Return Await Task.Run(Function()
                                  Try
                                      Using searcher As New ManagementObjectSearcher(query)
                                          ' Vérifier à nouveau si une annulation a été demandée
                                          If CancellationRequested Then
                                              Return Nothing
                                          End If
                                          Return searcher.Get()
                                      End Using
                                  Catch ex As Exception
                                      ' Gérer les erreurs WMI
                                      Debug.WriteLine($"Erreur WMI: {ex.Message}")
                                      Return Nothing
                                  End Try
                              End Function)
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