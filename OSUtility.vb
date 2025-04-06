Imports System.Text
Imports Microsoft.Win32
Imports System.Management
Imports System.Globalization
Imports System.IO
Imports System.CodeDom.Compiler
Imports Microsoft.VisualBasic.Devices  ' Ajout de l'importation manquante pour ComputerInfo

''' <summary>
''' Classe utilitaire pour obtenir des informations détaillées sur le système d'exploitation Windows.
''' </summary>
Public Class OSUtility

    ''' <summary>
    ''' Génère un rapport détaillé sur le système d'exploitation Windows.
    ''' </summary>
    Public Shared Function GetDetailedReport() As String
        Dim report As New StringBuilder()

        report.AppendLine("=== INFORMATIONS DÉTAILLÉES SUR LE SYSTÈME ===")
        report.AppendLine()

        ' Informations de base sur Windows
        report.AppendLine("== SYSTÈME D'EXPLOITATION ==")
        report.AppendLine($"  • Nom: {Environment.OSVersion.VersionString}")
        report.AppendLine($"  • Version: {GetWindowsVersion()}")
        report.AppendLine($"  • Architecture: {GetSystemArchitecture()}")
        report.AppendLine($"  • Nom machine: {Environment.MachineName}")
        report.AppendLine($"  • Nom utilisateur: {Environment.UserName}")
        report.AppendLine($"  • Domaine: {Environment.UserDomainName}")
        report.AppendLine()

        ' Informations système
        report.AppendLine("== RESSOURCES SYSTÈME ==")
        report.AppendLine($"  • Processeurs logiques: {Environment.ProcessorCount}")
        report.AppendLine($"  • Mémoire système: {GetTotalPhysicalMemory()}")
        report.AppendLine($"  • Espace disque système: {GetSystemDriveSpace()}")
        report.AppendLine($"  • Temps depuis démarrage: {GetSystemUptime()}")
        report.AppendLine()

        ' Informations régionales
        report.AppendLine("== PARAMÈTRES RÉGIONAUX ==")
        report.AppendLine($"  • Culture système: {CultureInfo.CurrentCulture.DisplayName}")
        report.AppendLine($"  • Culture UI: {CultureInfo.CurrentUICulture.DisplayName}")
        report.AppendLine($"  • Format date: {CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern}")
        report.AppendLine($"  • Format heure: {CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern}")
        report.AppendLine()

        ' Informations réseau
        report.AppendLine("== RÉSEAU ==")
        report.AppendLine($"  • Nom hôte: {System.Net.Dns.GetHostName()}")
        Try
            Dim hostEntry = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName())
            report.AppendLine("  • Adresses IP:")
            For Each ip In hostEntry.AddressList
                If ip.AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork Then
                    report.AppendLine($"      - {ip}")
                End If
            Next
        Catch ex As Exception
            report.AppendLine("  • Impossible d'obtenir les adresses IP")
        End Try
        report.AppendLine()

        ' Informations matérielles via méthodes alternatives
        report.AppendLine("== INFORMATIONS MATÉRIELLES ==")

        ' CPU
        Try
            report.AppendLine("  • CPU:")

            ' Méthode alternative pour obtenir les informations CPU
            Using searcher As New ManagementObjectSearcher("root\CIMV2", "SELECT * FROM Win32_Processor")
                For Each queryObj As ManagementObject In searcher.Get()
                    report.AppendLine($"      - Nom: {SafeGetProperty(queryObj, "Name")}")
                    report.AppendLine($"      - Fabricant: {SafeGetProperty(queryObj, "Manufacturer")}")
                    report.AppendLine($"      - Fréquence: {SafeGetProperty(queryObj, "MaxClockSpeed")} MHz")
                    report.AppendLine($"      - Cores: {SafeGetProperty(queryObj, "NumberOfCores")}")
                    report.AppendLine($"      - Threads: {SafeGetProperty(queryObj, "NumberOfLogicalProcessors")}")
                Next
            End Using
        Catch ex As Exception
            report.AppendLine($"      - Impossible d'obtenir les informations CPU: {ex.Message}")
        End Try
        report.AppendLine()

        ' Carte graphique
        Try
            report.AppendLine("  • Carte graphique:")

            Using searcher As New ManagementObjectSearcher("root\CIMV2", "SELECT * FROM Win32_VideoController")
                For Each queryObj As ManagementObject In searcher.Get()
                    report.AppendLine($"      - Nom: {SafeGetProperty(queryObj, "Name")}")

                    Dim adapterRAM = SafeGetProperty(queryObj, "AdapterRAM")
                    If Not String.IsNullOrEmpty(adapterRAM) AndAlso IsNumeric(adapterRAM) Then
                        Dim ramMB As Double = Convert.ToDouble(adapterRAM) / (1024 * 1024)
                        report.AppendLine($"      - RAM: {ramMB:N0} MB")
                    End If

                    Dim hRes = SafeGetProperty(queryObj, "CurrentHorizontalResolution")
                    Dim vRes = SafeGetProperty(queryObj, "CurrentVerticalResolution")

                    If Not String.IsNullOrEmpty(hRes) AndAlso Not String.IsNullOrEmpty(vRes) Then
                        report.AppendLine($"      - Résolution actuelle: {hRes} x {vRes}")
                    End If
                Next
            End Using
        Catch ex As Exception
            report.AppendLine($"      - Impossible d'obtenir les informations de carte graphique: {ex.Message}")
        End Try
        report.AppendLine()

        ' BIOS
        Try
            report.AppendLine("  • BIOS:")

            Using searcher As New ManagementObjectSearcher("root\CIMV2", "SELECT * FROM Win32_BIOS")
                For Each queryObj As ManagementObject In searcher.Get()
                    report.AppendLine($"      - Fabricant: {SafeGetProperty(queryObj, "Manufacturer")}")
                    report.AppendLine($"      - Version: {SafeGetProperty(queryObj, "Version")}")
                    report.AppendLine($"      - Date: {SafeGetProperty(queryObj, "ReleaseDate")}")
                Next
            End Using
        Catch ex As Exception
            report.AppendLine($"      - Impossible d'obtenir les informations BIOS: {ex.Message}")
        End Try
        report.AppendLine()

        ' Carte mère
        Try
            report.AppendLine("  • Carte mère:")

            Using searcher As New ManagementObjectSearcher("root\CIMV2", "SELECT * FROM Win32_BaseBoard")
                For Each queryObj As ManagementObject In searcher.Get()
                    report.AppendLine($"      - Fabricant: {SafeGetProperty(queryObj, "Manufacturer")}")
                    report.AppendLine($"      - Modèle: {SafeGetProperty(queryObj, "Product")}")
                    report.AppendLine($"      - Numéro de série: {SafeGetProperty(queryObj, "SerialNumber")}")
                Next
            End Using
        Catch ex As Exception
            report.AppendLine($"      - Impossible d'obtenir les informations de carte mère: {ex.Message}")
        End Try

        Return report.ToString()
    End Function

    ''' <summary>
    ''' Obtient la version détaillée de Windows, avec détection spéciale pour Windows 11.
    ''' </summary>
    Private Shared Function GetWindowsVersion() As String
        Try
            ' Méthode spécifique pour détecter Windows 11
            Dim reg = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion")
            If reg IsNot Nothing Then
                Dim productName = reg.GetValue("ProductName", "").ToString()
                Dim displayVersion = reg.GetValue("DisplayVersion", "").ToString()
                Dim currentBuild = reg.GetValue("CurrentBuild", "").ToString()

                ' Détection de Windows 11 basée sur le numéro de build
                If Not String.IsNullOrEmpty(currentBuild) Then
                    Dim buildNumber As Integer
                    If Integer.TryParse(currentBuild, buildNumber) AndAlso buildNumber >= 22000 Then
                        ' Windows 11
                        Return $"Windows 11 {productName.Replace("Windows 10", "")} ({displayVersion}, build {currentBuild})"
                    Else
                        ' Windows 10 ou antérieur
                        Return $"{productName} ({displayVersion}, build {currentBuild})"
                    End If
                Else
                    Return $"{productName} (build inconnu)"
                End If
            End If

            Return Environment.OSVersion.VersionString
        Catch ex As Exception
            Return Environment.OSVersion.VersionString
        End Try
    End Function

    ''' <summary>
    ''' Obtient l'architecture du système (32 ou 64 bits).
    ''' </summary>
    Private Shared Function GetSystemArchitecture() As String
        If Environment.Is64BitOperatingSystem Then
            Return "64-bit"
        Else
            Return "32-bit"
        End If
    End Function

    ''' <summary>
    ''' Obtient la quantité totale de mémoire physique.
    ''' </summary>
    Private Shared Function GetTotalPhysicalMemory() As String
        Try
            Dim memoryInfo = New ComputerInfo()
            Return FormatBytes(memoryInfo.TotalPhysicalMemory)
        Catch ex As Exception
            ' Méthode alternative
            Try
                Using searcher As New ManagementObjectSearcher("root\CIMV2", "SELECT * FROM Win32_ComputerSystem")
                    For Each queryObj As ManagementObject In searcher.Get()
                        Dim memoryBytes = SafeGetProperty(queryObj, "TotalPhysicalMemory")
                        If Not String.IsNullOrEmpty(memoryBytes) AndAlso IsNumeric(memoryBytes) Then
                            Return FormatBytes(Convert.ToUInt64(memoryBytes))
                        End If
                    Next
                End Using
            Catch
                ' Ignorer
            End Try

            Return "Inconnu"
        End Try
    End Function

    ''' <summary>
    ''' Obtient l'espace disponible sur le lecteur système.
    ''' </summary>
    Private Shared Function GetSystemDriveSpace() As String
        Try
            Dim systemDrive As String = Environment.GetEnvironmentVariable("SystemDrive")
            Dim driveInfo As New DriveInfo(systemDrive)

            Dim total = FormatBytes(driveInfo.TotalSize)
            Dim available = FormatBytes(driveInfo.AvailableFreeSpace)

            Return $"{available} disponibles sur {total}"
        Catch ex As Exception
            Return "Inconnu"
        End Try
    End Function

    ''' <summary>
    ''' Obtient le temps écoulé depuis le démarrage du système.
    ''' </summary>
    Private Shared Function GetSystemUptime() As String
        Try
            Dim uptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
            Return $"{uptime.Days} jours, {uptime.Hours} heures, {uptime.Minutes} minutes"
        Catch ex As Exception
            ' Méthode alternative
            Try
                Using searcher As New ManagementObjectSearcher("root\CIMV2", "SELECT * FROM Win32_OperatingSystem")
                    For Each queryObj As ManagementObject In searcher.Get()
                        Dim lastBoot = SafeGetProperty(queryObj, "LastBootUpTime")
                        If Not String.IsNullOrEmpty(lastBoot) Then
                            Dim bootTime = ManagementDateTimeConverter.ToDateTime(lastBoot)
                            Dim uptime = DateTime.Now - bootTime
                            Return $"{uptime.Days} jours, {uptime.Hours} heures, {uptime.Minutes} minutes"
                        End If
                    Next
                End Using
            Catch
                ' Ignorer
            End Try

            Return "Inconnu"
        End Try
    End Function

    ''' <summary>
    ''' Formate une taille en octets en unité lisible (Ko, Mo, Go, To).
    ''' </summary>
    Private Shared Function FormatBytes(bytes As ULong) As String
        Const unit As ULong = 1024
        If bytes < unit Then
            Return $"{bytes} octets"
        End If

        Dim exp = CInt(Math.Log(bytes, unit))
        Dim size = bytes / Math.Pow(unit, exp)

        Dim suffix As String = "KMGTPE".Substring(exp - 1, 1)
        Return $"{size:N2} {suffix}o"
    End Function

    ''' <summary>
    ''' Récupère une propriété d'un objet WMI de manière sécurisée.
    ''' </summary>
    Private Shared Function SafeGetProperty(obj As ManagementObject, propertyName As String) As String
        Try
            If obj(propertyName) IsNot Nothing Then
                Return obj(propertyName).ToString()
            End If
        Catch
            ' Ignorer les erreurs
        End Try

        Return "Non disponible"
    End Function
End Class