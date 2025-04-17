Imports System.Text
Imports Microsoft.Win32
Imports System.Management
Imports System.IO
Imports System.Threading.Tasks

''' <summary>
''' Classe utilitaire pour obtenir des informations détaillées sur le système d'exploitation Windows.
''' </summary>
Public Class OSUtility
    Inherits BaseInfoCollector

    ' Nombre total d'étapes dans la collecte d'informations système
    Private Const TOTAL_STEPS As Integer = 3

    ''' <summary>
    ''' Génère un rapport détaillé sur le système d'exploitation Windows de façon asynchrone.
    ''' </summary>
    Public Shared Async Function GetReportAsync() As Task(Of String)
        Return Await Task.Run(Function() GetReport())
    End Function

    ''' <summary>
    ''' Génère un rapport détaillé sur le système d'exploitation Windows.
    ''' </summary>
    Public Shared Function GetReport() As String
        ' StringBuilder pour construire efficacement le rapport
        Dim report As New StringBuilder()
        ' Compteur d'étapes pour suivre la progression
        Dim currentStep As Integer = 0

        ' En-tête du rapport
        report.AppendLine("=== INFORMATIONS SYSTÈME D'EXPLOITATION ===")
        report.AppendLine()

        ' ===== ÉTAPE 1: INFORMATIONS DE BASE SUR WINDOWS =====
        currentStep += 1
        ' Notification de progression pour mettre à jour l'UI
        NotifyProgress("Collecte des informations système de base...", currentStep, TOTAL_STEPS)

        ' Vérifier si on doit continuer
        If Not ApplyDelayAndCheckCancellation() Then
            report.AppendLine()
            report.AppendLine("== COLLECTE INTERROMPUE PAR L'UTILISATEUR ==")
            Return report.ToString()
        End If

        report.AppendLine("== SYSTÈME D'EXPLOITATION ==")
        report.AppendLine($"  • Nom: {Environment.OSVersion.VersionString}")
        report.AppendLine($"  • Version: {GetWindowsVersion()}")
        report.AppendLine($"  • Architecture: {GetSystemArchitecture()}")
        report.AppendLine($"  • Nom machine: {Environment.MachineName}")
        report.AppendLine()

        ' ===== ÉTAPE 2: INFORMATIONS SYSTÈME =====
        currentStep += 1
        NotifyProgress("Collecte des informations sur les ressources système...", currentStep, TOTAL_STEPS)

        ' Vérifier si on doit continuer
        If Not ApplyDelayAndCheckCancellation() Then
            report.AppendLine()
            report.AppendLine("== COLLECTE INTERROMPUE PAR L'UTILISATEUR ==")
            Return report.ToString()
        End If

        report.AppendLine("== RESSOURCES SYSTÈME ==")
        report.AppendLine($"  • Processeurs logiques: {Environment.ProcessorCount}")
        report.AppendLine($"  • Mémoire système: {GetTotalPhysicalMemory()}")
        report.AppendLine($"  • Espace disque système: {GetSystemDriveSpace()}")
        report.AppendLine($"  • Temps depuis démarrage: {GetSystemUptime()}")
        report.AppendLine()

        ' ===== ÉTAPE 3: INFORMATIONS RÉSEAU =====
        currentStep += 1
        NotifyProgress("Collecte des informations réseau...", currentStep, TOTAL_STEPS)

        ' Vérifier si on doit continuer
        If Not ApplyDelayAndCheckCancellation() Then
            report.AppendLine()
            report.AppendLine("== COLLECTE INTERROMPUE PAR L'UTILISATEUR ==")
            Return report.ToString()
        End If

        report.AppendLine("== RÉSEAU ==")
        report.AppendLine("** Affichage simplifié de la carte réseau active **")
        report.AppendLine($"  • Nom hôte: {System.Net.Dns.GetHostName()}")
        ' Affichage simplifié de la carte réseau active
        Try
            Dim hostEntry = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName())
            report.AppendLine("  • Adresses IP:")
            For Each ip In hostEntry.AddressList
                ' Ne montrer que les adresses IPv4 pour plus de clarté
                If ip.AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork Then
                    report.AppendLine($"      - {ip}")
                End If
            Next
        Catch ex As Exception
            report.AppendLine("  • Impossible d'obtenir les adresses IP")
        End Try

        ' Vérifier si on doit continuer
        If Not ApplyDelayAndCheckCancellation() Then
            report.AppendLine()
            report.AppendLine("== COLLECTE INTERROMPUE PAR L'UTILISATEUR ==")
            Return report.ToString()
        End If

        Try
            report.AppendLine("")
            report.AppendLine("** Affichage complet de toutes les cartes réseaux **")
            report.AppendLine("  • Interfaces réseau:")
            For Each networkInterface In System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                ' Ignorer les interfaces désactivées ou non opérationnelles
                If networkInterface.OperationalStatus <> System.Net.NetworkInformation.OperationalStatus.Up Then
                    Continue For
                End If

                report.AppendLine($"      - {networkInterface.Name} ({networkInterface.Description}):")

                ' Récupérer les adresses IP directement
                Dim properties = networkInterface.GetIPProperties()
                For Each address In properties.UnicastAddresses
                    If address.Address.AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork Then
                        report.AppendLine($"          IP: {address.Address}, Masque: {address.IPv4Mask}")
                    End If
                Next
            Next
        Catch ex As Exception
            report.AppendLine($"  • Impossible d'obtenir les informations réseau: {ex.Message}")
        End Try

        report.AppendLine()

        ' Notification finale de progression
        NotifyProgress("Traitement terminé!", TOTAL_STEPS, TOTAL_STEPS)

        ' Retourner le rapport complet
        Return report.ToString()
    End Function

    ' ========== MÉTHODES UTILITAIRES POUR LA COLLECTE D'INFORMATIONS ==========

    ''' <summary>
    ''' Obtient la version détaillée de Windows, avec détection spéciale pour Windows 11.
    ''' </summary>
    Private Shared Function GetWindowsVersion() As String
        Try
            ' Accéder à la clé de registre qui contient les informations de version de Windows
            Dim reg = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion")
            If reg IsNot Nothing Then
                ' Extraire les informations clés
                Dim productName = reg.GetValue("ProductName", "").ToString()
                Dim displayVersion = reg.GetValue("DisplayVersion", "").ToString()
                Dim currentBuild = reg.GetValue("CurrentBuild", "").ToString()

                ' Détection de Windows 11 basée sur le numéro de build
                ' Windows 11 commence à partir du build 22000
                If Not String.IsNullOrEmpty(currentBuild) Then
                    Dim buildNumber As Integer
                    If Integer.TryParse(currentBuild, buildNumber) AndAlso buildNumber >= 22000 Then
                        ' C'est Windows 11
                        Return $"Windows 11 {productName.Replace("Windows 10", "")} ({displayVersion}, build {currentBuild})"
                    Else
                        ' C'est Windows 10 ou une version antérieure
                        Return $"{productName} ({displayVersion}, build {currentBuild})"
                    End If
                Else
                    Return $"{productName} (build inconnu)"
                End If
            End If

            ' Méthode de secours si l'accès au registre échoue
            Return Environment.OSVersion.VersionString
        Catch ex As Exception
            ' En cas d'erreur, utiliser la méthode standard
            Return Environment.OSVersion.VersionString
        End Try
    End Function

    ''' <summary>
    ''' Détermine l'architecture du système (32 ou 64 bits).
    ''' </summary>
    Private Shared Function GetSystemArchitecture() As String
        ' Vérifier si le système est 64 bits
        If Environment.Is64BitOperatingSystem Then
            Return "64-bit"
        Else
            Return "32-bit"
        End If
    End Function

    ''' <summary>
    ''' Obtient la quantité totale de mémoire physique installée sur le système.
    ''' </summary>
    Private Shared Function GetTotalPhysicalMemory() As String
        Try
            ' Utiliser WMI pour interroger les informations système
            Using searcher As New ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem")
                For Each queryObj As ManagementObject In searcher.Get()
                    ' Récupérer la propriété TotalPhysicalMemory de manière sécurisée
                    Dim memoryBytes = SafeGetProperty(queryObj, "TotalPhysicalMemory")
                    If Not String.IsNullOrEmpty(memoryBytes) AndAlso IsNumeric(memoryBytes) Then
                        ' Convertir la valeur en un format lisible (Ko, Mo, Go, etc.)
                        Return FormatBytes(Convert.ToUInt64(memoryBytes))
                    End If
                Next
            End Using
        Catch ex As Exception
            ' En cas d'erreur, retourner une valeur par défaut
            Return "Inconnu"
        End Try

        Return "Inconnu"
    End Function

    ''' <summary>
    ''' Obtient l'espace disponible sur le lecteur système (généralement C:).
    ''' </summary>
    Private Shared Function GetSystemDriveSpace() As String
        Try
            ' Récupérer la lettre du lecteur système depuis les variables d'environnement
            Dim systemDrive As String = Environment.GetEnvironmentVariable("SystemDrive")
            ' Créer un objet DriveInfo pour accéder aux informations du lecteur
            Dim driveInfo As New DriveInfo(systemDrive)

            ' Formater l'espace total et l'espace disponible
            Dim total = FormatBytes(driveInfo.TotalSize)
            Dim available = FormatBytes(driveInfo.AvailableFreeSpace)

            Return $"{available} disponibles sur {total}"
        Catch ex As Exception
            Return "Inconnu"
        End Try
    End Function

    ''' <summary>
    ''' Calcule le temps écoulé depuis le dernier démarrage du système.
    ''' </summary>
    Private Shared Function GetSystemUptime() As String
        Try
            ' Méthode principale: utiliser TickCount64 (nombre de millisecondes depuis le démarrage)
            Dim uptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
            Return $"{uptime.Days} jours, {uptime.Hours} heures, {uptime.Minutes} minutes"
        Catch ex As Exception
            ' Méthode alternative si la première échoue: utiliser WMI
            Try
                Using searcher As New ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem")
                    For Each queryObj As ManagementObject In searcher.Get()
                        ' Récupérer la date/heure du dernier démarrage
                        Dim lastBoot = SafeGetProperty(queryObj, "LastBootUpTime")
                        If Not String.IsNullOrEmpty(lastBoot) Then
                            ' Convertir la date WMI en DateTime
                            Dim bootTime = ManagementDateTimeConverter.ToDateTime(lastBoot)
                            ' Calculer le temps écoulé depuis
                            Dim uptime = DateTime.Now - bootTime
                            Return $"{uptime.Days} jours, {uptime.Hours} heures, {uptime.Minutes} minutes"
                        End If
                    Next
                End Using
            Catch
                ' Ignorer les erreurs de la méthode alternative
            End Try

            Return "Inconnu"
        End Try
    End Function
End Class