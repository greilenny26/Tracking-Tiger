using Tracking_Tiger.Core.ControlAcceso;

namespace TrackingTiger.Pruebas.ControlAcceso;

// Pruebas de la política mínima de contraseña (RF-CA-14):
// al menos 8 caracteres, con letras y números.
public class PoliticaContrasenaPruebas
{
    [Fact]
    public void Validar_ContrasenaConLetrasYNumeros_EsAceptada()
    {
        // Arrange
        var contrasena = "tigre2007";

        // Act
        var resultado = PoliticaContrasena.Validar(contrasena);

        // Assert
        Assert.True(resultado.Valido);
        Assert.Null(resultado.Mensaje);
    }

    [Fact]
    public void Validar_ContrasenaDeOchoCaracteresExactos_EsAceptada()
    {
        // Arrange
        var contrasena = "Jhadiel7"; // 8 caracteres

        // Act
        var resultado = PoliticaContrasena.Validar(contrasena);

        // Assert
        Assert.True(resultado.Valido);
    }

    [Fact]
    public void Validar_ContrasenaDeSieteCaracteres_EsRechazadaPorLargo()
    {
        // Arrange
        var contrasena = "Jhadiel";

        // Act
        var resultado = PoliticaContrasena.Validar(contrasena);

        // Assert
        Assert.False(resultado.Valido);
        Assert.Equal("La contraseña debe tener al menos 8 caracteres.", resultado.Mensaje);
    }
}
