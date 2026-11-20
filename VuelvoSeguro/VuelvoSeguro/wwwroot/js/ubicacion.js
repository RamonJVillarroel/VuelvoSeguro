// Origen del viaje tomado de la ubicación del celular (navigator.geolocation, sin servicios pagos).
// - Si el pasajero da permiso: se guardan latitud/longitud y se intenta completar una dirección
//   aproximada con OpenStreetMap (Nominatim, gratis). El texto pasa a ser opcional.
// - Si niega el permiso o el navegador no puede: el texto "Origen" queda obligatorio, como respaldo.
// La geolocalización solo funciona en HTTPS (o en localhost).
(function () {
    var form = document.getElementById('form-publicar');
    if (!form) {
        return;
    }
    var origen = document.getElementById('origen');
    var latitud = document.getElementById('latitud');
    var longitud = document.getElementById('longitud');
    var estado = document.getElementById('ubicacion-estado');
    var reintentar = document.getElementById('ubicacion-reintentar');

    var buscando = false;
    var enviarAlTerminar = false;
    var origenEscritoPorLaPersona = origen.value.trim() !== '';

    origen.addEventListener('input', function () {
        origenEscritoPorLaPersona = true;
    });

    function mostrar(texto, clase) {
        estado.textContent = texto;
        estado.className = 'small mt-1 ' + (clase || 'text-body-secondary');
    }

    function sinUbicacion(texto) {
        buscando = false;
        latitud.value = '';
        longitud.value = '';
        origen.required = true;
        origen.placeholder = 'Ej.: Bar La Esquina, Av. Corrientes 1800';
        mostrar(texto, 'text-danger');
        reintentar.classList.toggle('d-none', !navigator.geolocation);
        if (enviarAlTerminar) {
            enviarAlTerminar = false;
            origen.focus();
        }
    }

    function conUbicacion(pos) {
        buscando = false;
        var lat = pos.coords.latitude;
        var lon = pos.coords.longitude;
        latitud.value = lat.toFixed(6);
        longitud.value = lon.toFixed(6);
        origen.required = false;
        origen.placeholder = 'Referencia opcional (ej.: puerta del bar)';
        reintentar.classList.add('d-none');
        var precision = Math.round(pos.coords.accuracy || 0);
        mostrar('Tomamos tu ubicación del celular' + (precision ? ' (±' + precision + ' m)' : '') +
            '. Si querés, agregá una referencia.', 'text-success');
        if (enviarAlTerminar) {
            enviarAlTerminar = false;
            form.submit();
            return;
        }
        completarDireccion(lat, lon);
    }

    // Dirección aproximada gratuita (OpenStreetMap). Si falla, alcanza con las coordenadas.
    function completarDireccion(lat, lon) {
        if (!window.fetch || origenEscritoPorLaPersona) {
            return;
        }
        var url = 'https://nominatim.openstreetmap.org/reverse?format=jsonv2&zoom=18&accept-language=es' +
            '&lat=' + encodeURIComponent(lat) + '&lon=' + encodeURIComponent(lon);
        fetch(url, { headers: { 'Accept': 'application/json' } })
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (datos) {
                var texto = armarDireccion(datos);
                if (texto && !origenEscritoPorLaPersona) {
                    origen.value = texto.substring(0, 150);
                }
            })
            .catch(function () { /* sin dirección: el conductor igual recibe el link al mapa */ });
    }

    function armarDireccion(datos) {
        if (!datos) {
            return '';
        }
        var a = datos.address || {};
        var calle = [a.road || a.pedestrian || a.footway, a.house_number].filter(Boolean).join(' ');
        var zona = a.suburb || a.neighbourhood || a.city_district || a.city || a.town || a.village;
        var lugar = datos.name && datos.name !== a.road ? datos.name : '';
        var partes = [lugar, calle, zona].filter(Boolean);
        return partes.length ? partes.join(', ') : (datos.display_name || '');
    }

    function buscar() {
        if (!navigator.geolocation) {
            sinUbicacion('Tu navegador no permite tomar la ubicación. Escribí dónde estás.');
            return;
        }
        if (!window.isSecureContext) {
            sinUbicacion('La ubicación solo funciona con conexión segura (https). Escribí dónde estás.');
            return;
        }
        buscando = true;
        reintentar.classList.add('d-none');
        mostrar('Buscando tu ubicación…');
        navigator.geolocation.getCurrentPosition(conUbicacion, function (error) {
            sinUbicacion(error.code === error.PERMISSION_DENIED
                ? 'No diste permiso de ubicación. Escribí dónde estás.'
                : 'No pudimos tomar tu ubicación. Escribí dónde estás.');
        }, { enableHighAccuracy: true, timeout: 15000, maximumAge: 60000 });
    }

    // Si toca "Volver a casa" mientras todavía se busca la ubicación, se espera y se publica solo.
    form.addEventListener('submit', function (e) {
        if (buscando && !latitud.value && origen.value.trim() === '') {
            e.preventDefault();
            enviarAlTerminar = true;
            mostrar('Un momento, estamos tomando tu ubicación para publicar el viaje…');
        }
    });

    reintentar.addEventListener('click', buscar);

    // Mientras busca, el texto no es obligatorio (la ubicación puede llegar en unos segundos).
    origen.required = false;
    buscar();
})();
