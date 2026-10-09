(function () {
    "use strict";

    function ensureToastContainer() {
        var el = document.getElementById("vigia-toast-container");
        if (el) return el;
        el = document.createElement("div");
        el.id = "vigia-toast-container";
        el.className = "toast-container position-fixed top-0 end-0 p-3";
        el.style.zIndex = "1090";
        document.body.appendChild(el);
        return el;
    }

    function showToast(title, body, variant) {
        var container = ensureToastContainer();
        var id = "toast-" + Date.now();
        var html =
            '<div id="' + id + '" class="toast vt-toast vt-toast--' + variant + '" role="alert" aria-live="assertive" aria-atomic="true">' +
            '  <div class="toast-header">' +
            '    <strong class="me-auto">' + title + '</strong>' +
            '    <small class="text-muted">ahora</small>' +
            '    <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Cerrar"></button>' +
            '  </div>' +
            '  <div class="toast-body">' + body + '</div>' +
            '</div>';
        container.insertAdjacentHTML("beforeend", html);
        var toastEl = document.getElementById(id);

        if (typeof bootstrap !== 'undefined') {
            var toast = new bootstrap.Toast(toastEl, { delay: 6000 });
            toast.show();
            toastEl.addEventListener("hidden.bs.toast", function () { toastEl.remove(); });
        }
    }

    function prependIncidenciaRow(data) {
        var table = document.getElementById("bandeja-incidencias");
        if (!table || !data) return;
        var tbody = table.querySelector("tbody");
        if (!tbody) return;
        var tr = document.createElement("tr");
        tr.className = "vt-row-new";
        tr.innerHTML =
            "<td><code class=\"vt-code\">" + (data.codigo || "") + "</code></td>" +
            "<td>" + (data.obraNombre || "") + "</td>" +
            "<td><span class=\"badge vt-badge-info\">" + (data.estado || "Pendiente") + "</span></td>" +
            "<td>" + (data.fecha || "") + "</td>" +
            "<td>0</td>" +
            "<td><a class=\"btn btn-sm btn-primary\" href=\"/Supervisor/Revisar/" + data.id + "\">Revisar</a></td>";
        tbody.insertBefore(tr, tbody.firstChild);

        var empty = document.querySelector(".vt-empty");
        if (empty) empty.remove();
    }

    function start() {
        if (typeof signalR === "undefined") {
            return;
        }

        var connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/vigia")
            .withAutomaticReconnect()
            .build();

        connection.on("NuevaIncidencia", function (data) {
            var msg = (data && data.mensaje) || "Nueva incidencia registrada";
            var detail = data ? (msg + ": " + (data.codigo || "") + " - " + (data.obraNombre || "")) : msg;
            showToast("Nueva incidencia", detail, "success");
            prependIncidenciaRow(data);
            
        });

        connection.on("IncidenciaActualizada", function (data) {
            var msg = (data && data.mensaje) || "Estado actualizado";
            var detail = data ? (msg + ": " + (data.codigo || "") + " -> " + (data.estado || "")) : msg;
            showToast("Incidencia actualizada", detail, "info");
        });

        connection.on("ObservacionAgregada", function (data) {
            var msg = (data && data.mensaje) || "Observación agregada";
            var detail = data ? (msg + " (" + (data.codigo || "") + ")") : msg;
            showToast("Observación", detail, "info");
        });

        connection.on("RespuestaSolicitud", function (data) {
            var msg = (data && data.mensaje) || "El Personal Municipal respondió una solicitud";
            var detail = data ? (msg + ": " + (data.codigo || "") + (data.obraNombre ? " - " + data.obraNombre : "")) : msg;
            showToast("Respuesta recibida", detail, "success");
        });

        connection.on("NuevaSolicitudInformacion", function (data) {
            var msg = (data && data.mensaje) || "Nueva solicitud de información";
            var detail = data ? (msg + ": " + (data.codigo || "") + (data.obraNombre ? " - " + data.obraNombre : "")) : msg;
            showToast("Solicitud del Supervisor", detail, "warning");
        });

        connection.on("EstadoObraActualizado", function (data) {
            var msg = (data && data.mensaje) || "Estado de obra actualizado";
            var detail = data ? (msg + ": " + (data.nombre || "") + (data.estado ? " [" + data.estado + "]" : "") + (data.avance != null ? " - avance " + data.avance + "%" : "")) : msg;
            showToast("Obra actualizada", detail, "warning");
        });

        connection.on("ArchivoObraActualizado", function (data) {
            var msg = (data && data.mensaje) || "Nueva evidencia publicada";
            var detail = data ? (msg + " - " + (data.archivoNombre || "")) : msg;
            showToast("Nueva evidencia", detail, "success");
            
            if (data && data.obraId) {
                var currentUrl = window.location.href;
                if (currentUrl.includes("/Obras/DetallePublico/" + data.obraId) || currentUrl.includes("/Obras/Details/" + data.obraId)) {
                    setTimeout(function() {
                        window.location.reload();
                    }, 2500);
                }
            }
        });

        connection.start()
            .catch(function (err) {
            });

    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", start);
    } else {
        start();
    }
})();