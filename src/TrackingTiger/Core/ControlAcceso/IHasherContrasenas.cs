namespace Tracking_Tiger.Core.ControlAcceso;

// Calcula y comprueba el hash de las contraseñas (RF-CA-02, RD-05).
// La contraseña nunca se guarda: solo el valor que devuelve Hashear.
public interface IHasherContrasenas
{
    string Hashear(string contrasena);
    bool Verificar(string contrasena, string hashGuardado);
}
