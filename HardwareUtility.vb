Imports System.Text
Imports Hardware.Info
Imports System.Threading.Tasks
Imports System.Management

''' <summary>
''' Classe utilitaire pour obtenir des informations détaillées sur le matériel de l'ordinateur.
''' </summary>
Public Class HardwareUtility
    Inherits BaseInfoCollector

    ''' <summary>
    ''' Structure qui définit une étape de collecte d'informations matérielles.
    ''' </summary>
    Private Structure HardwareCollectionStep
        Public Name As String
        Public Description As String
    End Structure

    ''' <summary>
    ''' Liste des étapes de collecte d'informations matérielles.
    ''' </summary>
    Private Shared ReadOnly HardwareSteps As New List(Of HardwareCollectionStep) From {
        New HardwareCollectionStep With {
            .Name = "CPU",
            .Description = "Collecte des informations matérielles pour le CPU..."
        },
        New HardwareCollectionStep With {
            .Name = "GPU",
            .Description = "Collecte des informations matérielles pour la carte graphique..."
        },
        New HardwareCollectionStep With {
            .Name = "BIOS",
            .Description = "Collecte des informations matérielles pour le BIOS..."
        },
        New HardwareCollectionStep With {
            .Name = "Motherboard",
            .Description = "Collecte des informations matérielles pour la carte mère..."
        }
    }

    ''' <summary>
    ''' Génère un rapport détaillé sur le matériel de façon asynchrone.
    ''' </summary>
    Public Shared Async Function GetReportAsync() As Task(Of String)
        Return Await Task.Run(Function() GetReport())
    End Function

    ''' <summary>
    ''' Génère un rapport détaillé sur le matériel.
    ''' </summary>
    Public Shared Function GetReport() As String
        ' StringBuilder pour construire efficacement le rapport
        Dim report As New StringBuilder()

        ' En-tête du rapport
        report.AppendLine("=== INFORMATIONS MATÉRIELLES ===")
        report.AppendLine()

        ' Nombre total d'étapes pour la progression
        Dim totalSteps As Integer = HardwareSteps.Count
        Dim currentStep As Integer = 0

        ' Préparer l'objet HardwareInfo une seule fois pour optimiser les performances
        Dim hardwareInfo = New HardwareInfo()

        ' Cette opération peut prendre quelques secondes
        NotifyProgress("Initialisation de la collecte matérielle...", 0, totalSteps)
        hardwareInfo.RefreshAll()

        ' Parcourir chaque étape définie dans la liste HardwareSteps
        For Each hwStep In HardwareSteps
            ' Incrémenter le compteur d'étapes
            currentStep += 1
            ' Notifier la progression avec le message spécifique à cette étape
            NotifyProgress(hwStep.Description, currentStep, totalSteps)

            ' Vérifier si une annulation a été demandée
            If Not ApplyDelayAndCheckCancellation() Then
                report.AppendLine()
                report.AppendLine("== COLLECTE INTERROMPUE PAR L'UTILISATEUR ==")
                Return report.ToString()
            End If

            ' Traiter différemment selon le type de matériel
            Select Case hwStep.Name
                Case "CPU"
                    ' ==== COLLECTE D'INFORMATIONS CPU ====
                    Try
                        report.AppendLine("== CPU ==")
                        For Each cpu In hardwareInfo.CpuList
                            ' Afficher les détails de chaque CPU trouvé
                            report.AppendLine($"  • Nom: {cpu.Name}")
                            report.AppendLine($"  • Fabricant: {cpu.Manufacturer}")
                            report.AppendLine($"  • Cores: {cpu.NumberOfCores}")
                            report.AppendLine($"  • Threads: {cpu.NumberOfLogicalProcessors}")
                        Next
                    Catch ex As Exception
                        ' Gérer les erreurs de manière élégante
                        report.AppendLine($"  • Impossible d'obtenir les informations CPU: {ex.Message}")
                    End Try
                    report.AppendLine()

                Case "GPU"
                    ' ==== COLLECTE D'INFORMATIONS GPU ====
                    Try
                        report.AppendLine("== CARTE GRAPHIQUE ==")
                        For Each gpu In hardwareInfo.VideoControllerList
                            ' Afficher les détails de chaque carte graphique trouvée
                            report.AppendLine($"  • Nom: {gpu.Name}")
                            ' Convertir les octets en MB pour plus de lisibilité
                            report.AppendLine($"  • RAM: {gpu.AdapterRAM / (1024 * 1024):N0} MB")
                            report.AppendLine($"  • Résolution actuelle: {gpu.CurrentHorizontalResolution} x {gpu.CurrentVerticalResolution}")
                        Next
                    Catch ex As Exception
                        report.AppendLine($"  • Impossible d'obtenir les informations de carte graphique: {ex.Message}")
                    End Try
                    report.AppendLine()

                Case "BIOS"
                    ' ==== COLLECTE D'INFORMATIONS BIOS ====
                    Try
                        report.AppendLine("== BIOS ==")
                        For Each bios In hardwareInfo.BiosList
                            report.AppendLine($"  • Fabricant: {bios.Manufacturer}")
                            report.AppendLine($"  • Version: {bios.Version}")
                            report.AppendLine($"  • Date: {bios.ReleaseDate}")
                        Next
                    Catch ex As Exception
                        report.AppendLine($"  • Impossible d'obtenir les informations BIOS: {ex.Message}")
                    End Try
                    report.AppendLine()

                Case "Motherboard"
                    ' ==== COLLECTE D'INFORMATIONS CARTE MÈRE ====
                    Try
                        report.AppendLine("== CARTE MÈRE ==")
                        For Each motherboard In hardwareInfo.MotherboardList
                            report.AppendLine($"  • Fabricant: {motherboard.Manufacturer}")
                            report.AppendLine($"  • Modèle: {motherboard.Product}")
                            report.AppendLine($"  • Numéro de série: {motherboard.SerialNumber}")
                        Next
                    Catch ex As Exception
                        report.AppendLine($"  • Impossible d'obtenir les informations de carte mère: {ex.Message}")
                    End Try
                    report.AppendLine()
            End Select
        Next

        ' Notification finale de progression
        NotifyProgress("Traitement terminé!", totalSteps, totalSteps)

        ' Retourner le rapport complet
        Return report.ToString()
    End Function
End Class