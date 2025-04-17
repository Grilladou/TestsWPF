Imports System.Text
Imports System.Globalization
Imports System.Threading.Tasks
Imports System.Management
Imports System.Security.Principal
Imports Microsoft.Win32

''' <summary>
''' Classe utilitaire pour obtenir des informations détaillées sur l'utilisateur et les paramètres régionaux.
''' </summary>
Public Class UserUtility
    Inherits BaseInfoCollector

    ' Nombre total d'étapes dans la collecte d'informations utilisateur
    Private Const TOTAL_STEPS As Integer = 3

    ''' <summary>
    ''' Génère un rapport détaillé sur l'utilisateur et les paramètres régionaux de façon asynchrone.
    ''' </summary>
    Public Shared Async Function GetReportAsync() As Task(Of String)
        Return Await Task.Run(Function() GetReport())
    End Function

    ''' <summary>
    ''' Génère un rapport détaillé sur l'utilisateur et les paramètres régionaux.
    ''' </summary>
    Public Shared Function GetReport() As String
        ' StringBuilder pour construire efficacement le rapport
        Dim report As New StringBuilder()
        ' Compteur d'étapes pour suivre la progression
        Dim currentStep As Integer = 0

        ' En-tête du rapport
        report.AppendLine("=== INFORMATIONS UTILISATEUR ET RÉGIONALES ===")
        report.AppendLine()

        ' ===== ÉTAPE 1: INFORMATIONS SUR L'UTILISATEUR =====
        currentStep += 1
        ' Notification de progression pour mettre à jour l'UI
        NotifyProgress("Collecte des informations utilisateur...", currentStep, TOTAL_STEPS)

        ' Vérifier si on doit continuer
        If Not ApplyDelayAndCheckCancellation() Then
            report.AppendLine()
            report.AppendLine("== COLLECTE INTERROMPUE PAR L'UTILISATEUR ==")
            Return report.ToString()
        End If

        report.AppendLine("== UTILISATEUR ==")
        report.AppendLine($"  • Nom d'utilisateur: {Environment.UserName}")
        report.AppendLine($"  • Nom de domaine: {Environment.UserDomainName}")

        ' Récupérer les informations sur les groupes et privilèges de l'utilisateur
        Try
            Dim identity = WindowsIdentity.GetCurrent()
            If identity IsNot Nothing Then
                Dim principal = New WindowsPrincipal(identity)
                report.AppendLine($"  • Est administrateur: {principal.IsInRole(WindowsBuiltInRole.Administrator)}")
                report.AppendLine($"  • SID: {identity.User.Value}")

                report.AppendLine("  • Groupes:")
                For Each group In identity.Groups
                    Try
                        Dim groupName = group.Translate(GetType(NTAccount)).Value
                        ' Filtrer pour n'afficher que les groupes pertinents
                        If Not groupName.Contains("S-1-") AndAlso
                           Not groupName.Contains("Everyone") AndAlso
                           Not groupName.Contains("BUILTIN") Then
                            report.AppendLine($"      - {groupName}")
                        End If
                    Catch
                        ' Ignorer les erreurs lors de la traduction des SID
                    End Try
                Next
            End If
        Catch ex As Exception
            report.AppendLine($"  • Impossible d'obtenir les informations de sécurité: {ex.Message}")
        End Try
        report.AppendLine()

        ' ===== ÉTAPE 2: INFORMATIONS RÉGIONALES =====
        currentStep += 1
        NotifyProgress("Collecte des informations régionales...", currentStep, TOTAL_STEPS)

        ' Vérifier si on doit continuer
        If Not ApplyDelayAndCheckCancellation() Then
            report.AppendLine()
            report.AppendLine("== COLLECTE INTERROMPUE PAR L'UTILISATEUR ==")
            Return report.ToString()
        End If

        report.AppendLine("== PARAMÈTRES RÉGIONAUX ==")
        ' Récupérer les informations sur la culture courante
        Dim currentCulture = CultureInfo.CurrentCulture
        Dim currentUICulture = CultureInfo.CurrentUICulture

        report.AppendLine($"  • Culture système: {currentCulture.Name} ({currentCulture.DisplayName})")
        report.AppendLine($"  • Culture interface: {currentUICulture.Name} ({currentUICulture.DisplayName})")
        report.AppendLine($"  • Format de date courte: {currentCulture.DateTimeFormat.ShortDatePattern}")
        report.AppendLine($"  • Format d'heure: {currentCulture.DateTimeFormat.ShortTimePattern}")
        report.AppendLine($"  • Séparateur décimal: '{currentCulture.NumberFormat.NumberDecimalSeparator}'")
        report.AppendLine($"  • Symbole monétaire: {currentCulture.NumberFormat.CurrencySymbol}")
        report.AppendLine()

        ' ===== ÉTAPE 3: INFORMATIONS SUR LES PARAMÈTRES DU SYSTÈME =====
        currentStep += 1
        NotifyProgress("Collecte des informations de personnalisation...", currentStep, TOTAL_STEPS)

        ' Vérifier si on doit continuer
        If Not ApplyDelayAndCheckCancellation() Then
            report.AppendLine()
            report.AppendLine("== COLLECTE INTERROMPUE PAR L'UTILISATEUR ==")
            Return report.ToString()
        End If

        report.AppendLine("== PERSONNALISATION ==")
        ' Récupérer des informations sur le thème et les paramètres de personnalisation
        Try
            ' Vérifier si le thème sombre est activé
            Dim isDarkTheme = False
            Using key = Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")
                If key IsNot Nothing Then
                    Dim value = key.GetValue("AppsUseLightTheme")
                    isDarkTheme = (value IsNot Nothing AndAlso CInt(value) = 0)
                End If
            End Using
            report.AppendLine($"  • Thème sombre activé: {isDarkTheme}")

            ' Récupérer le fond d'écran
            Dim wallpaperPath = String.Empty
            Using key = Registry.CurrentUser.OpenSubKey("Control Panel\Desktop")
                If key IsNot Nothing Then
                    wallpaperPath = key.GetValue("WallPaper", "Non défini").ToString()
                End If
            End Using
            report.AppendLine($"  • Fond d'écran: {wallpaperPath}")

            ' Récupérer la résolution d'écran du moniteur principal
            Dim primaryScreen = System.Windows.Forms.Screen.PrimaryScreen
            If primaryScreen IsNot Nothing Then
                report.AppendLine($"  • Résolution écran principal: {primaryScreen.Bounds.Width} x {primaryScreen.Bounds.Height}")
                report.AppendLine($"  • Densité de pixels (DPI): {GetDpiForScreen(primaryScreen)}")
            End If
        Catch ex As Exception
            report.AppendLine($"  • Impossible d'obtenir les informations de personnalisation: {ex.Message}")
        End Try

        ' Récupérer des informations supplémentaires sur les fichiers temporaires de l'utilisateur
        Try
            Dim tempPath = System.IO.Path.GetTempPath()
            Dim tempFiles = System.IO.Directory.GetFiles(tempPath, "*.*", System.IO.SearchOption.TopDirectoryOnly)
            Dim tempDirs = System.IO.Directory.GetDirectories(tempPath, "*.*", System.IO.SearchOption.TopDirectoryOnly)

            report.AppendLine($"  • Dossier temporaire: {tempPath}")
            report.AppendLine($"  • Nombre de fichiers temporaires: {tempFiles.Length}")
            report.AppendLine($"  • Nombre de dossiers temporaires: {tempDirs.Length}")
        Catch ex As Exception
            report.AppendLine($"  • Impossible d'accéder aux fichiers temporaires: {ex.Message}")
        End Try
        report.AppendLine()

        ' Notification finale de progression
        NotifyProgress("Traitement terminé!", TOTAL_STEPS, TOTAL_STEPS)

        ' Retourner le rapport complet
        Return report.ToString()
    End Function

    ' ========== MÉTHODES UTILITAIRES POUR LA COLLECTE D'INFORMATIONS ==========

    ''' <summary>
    ''' Obtient la valeur DPI pour un écran spécifique.
    ''' </summary>
    Private Shared Function GetDpiForScreen(screen As System.Windows.Forms.Screen) As Integer
        Try
            ' Essayer d'obtenir le DPI via WMI
            Using searcher As New ManagementObjectSearcher("SELECT * FROM Win32_DesktopMonitor")
                Dim dpiValue As Integer = 96 ' Valeur par défaut

                For Each queryObj As ManagementObject In searcher.Get()
                    ' Si on trouve une propriété PixelsPerXLogicalInch, l'utiliser
                    Dim pixelsPerXLogicalInch = SafeGetProperty(queryObj, "PixelsPerXLogicalInch")
                    If Not String.IsNullOrEmpty(pixelsPerXLogicalInch) AndAlso IsNumeric(pixelsPerXLogicalInch) Then
                        dpiValue = Convert.ToInt32(pixelsPerXLogicalInch)
                        Exit For
                    End If
                Next

                Return dpiValue
            End Using
        Catch ex As Exception
            ' En cas d'erreur, retourner la valeur DPI standard
            Return 96
        End Try
    End Function
End Class