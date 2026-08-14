// Configuration Karma minimale pour la suite Angular.
//
// Le lancement reel exige un navigateur (Chrome/ChromeHeadless) : Forge.NET n'en fournit
// aucun, la preuve est donc declaree par l'apprenant. Ce fichier decrit comment la suite
// tournerait en local apres npm ci.
module.exports = function (config) {
  config.set({
    basePath: "",
    frameworks: ["jasmine", "@angular-devkit/build-angular"],
    plugins: [
      require("karma-jasmine"),
      require("karma-chrome-launcher"),
      require("karma-jasmine-html-reporter"),
      require("karma-coverage"),
      require("@angular-devkit/build-angular/plugins/karma"),
    ],
    reporters: ["progress", "kjhtml"],
    browsers: ["ChromeHeadless"],
    restartOnFileChange: true,
    singleRun: true,
  });
};
