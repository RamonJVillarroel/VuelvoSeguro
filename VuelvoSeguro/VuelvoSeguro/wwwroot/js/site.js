// Actualización automática de las pantallas del viaje (no hay notificaciones en tiempo real).
// La vista indica cada cuántos segundos con <meta name="vs-actualizar" content="20">.
// Se usa en lugar de <meta http-equiv="refresh"> porque así no se recarga la página
// mientras la persona está tocando un botón o escribiendo, y se comporta igual en todos los navegadores.
(function () {
    var meta = document.querySelector('meta[name="vs-actualizar"]');
    var segundos = meta ? parseInt(meta.content, 10) : 0;
    if (!segundos) {
        return;
    }
    function programar() {
        setTimeout(function () {
            var activo = document.activeElement;
            var escribiendo = activo && /^(INPUT|TEXTAREA|SELECT)$/.test(activo.tagName);
            if (document.hidden || escribiendo) {
                programar(); // se vuelve a intentar en el próximo ciclo
            } else {
                window.location.reload();
            }
        }, segundos * 1000);
    }
    programar();
})();
