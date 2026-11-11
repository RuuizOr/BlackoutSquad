// Vibración en el navegador del celular (Chrome/Android). Si el navegador no la soporta, no hace nada.
mergeInto(LibraryManager.library, {
  VibrarNavegador: function (milisegundos) {
    try {
      if (navigator && navigator.vibrate) navigator.vibrate(milisegundos);
    } catch (e) { }
  }
});
