# Vérifier la signature HMAC d'un webhook

Implémentez `Submission.IsWebhookSignatureValid(int timestamp, string rawBody, string secret,
string presentedSignatureHex)`.

Vous recevez un webhook. L'émetteur et vous partagez un secret ; il a signé son envoi en calculant
le condensat HMAC-SHA256 de la chaîne `horodatage.corps` et l'a joint en hexadécimal. La méthode
recalcule cette signature et la compare à celle présentée.

Règles exactes :

- la chaîne signée est l'horodatage (en base dix) suivi d'un point, puis le corps *brut* — soit
  `timestamp` puis `.` puis `rawBody`, concaténés dans cet ordre ;
- le condensat se calcule en HMAC-SHA256 sur les octets UTF-8 de cette chaîne, avec `secret` ;
- le corps se prend *tel qu'il est reçu* : ne le re-sérialisez jamais, cela en changerait la forme
  et ferait échouer une signature authentique ;
- la signature présentée est en hexadécimal : décodez-la ; un décodage impossible ou une longueur
  incohérente rend `false`, sans exception ;
- la comparaison des deux condensats se fait en temps constant.

Les valeurs des tests sont factices. Écrivez avant le code : un envoi intact, un corps altéré d'un
caractère, un mauvais secret, et une signature tronquée.

Exemple : entrée `[1749990000, "{\"event\":\"paid\"}", "forge-fake-webhook-secret",
"la signature hexadécimale correspondante"]`, sortie `true`.
