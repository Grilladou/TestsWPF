Imports System
Imports System.Globalization
Imports System.Windows
Imports System.Windows.Data

Namespace HelloWorldLeChat

    ' Convertisseur pour convertir un Boolean en Visibility (Visible / Collapsed)
    Public Class BooleanToVisibilityConverter
        Implements IValueConverter

        Public Function Convert(ByVal value As Object, ByVal targetType As Type, ByVal parameter As Object, ByVal culture As CultureInfo) As Object Implements IValueConverter.Convert
            Return If(CBool(value), Visibility.Visible, Visibility.Collapsed)
        End Function

        Public Function ConvertBack(ByVal value As Object, ByVal targetType As Type, ByVal parameter As Object, ByVal culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Return value.Equals(Visibility.Visible)
        End Function

    End Class

End Namespace
