Imports System.Globalization
Imports System.Windows
Imports System.Windows.Data

Namespace HelloWorldLeChat
    ''' <summary>
    ''' Convertisseur pour transformer une valeur booléenne en Visibility
    ''' </summary>
    Public Class BooleanToVisibilityConverter
        Implements IValueConverter

        ''' <summary>
        ''' Convertit une valeur booléenne en Visibility
        ''' </summary>
        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            ' Si la valeur est True, renvoyer Visible, sinon Collapsed
            Return If(TypeOf value Is Boolean AndAlso CBool(value), Visibility.Visible, Visibility.Collapsed)
        End Function

        ''' <summary>
        ''' Convertit une valeur Visibility en booléen
        ''' </summary>
        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Return TypeOf value Is Visibility AndAlso value.Equals(Visibility.Visible)
        End Function
    End Class
End Namespace
