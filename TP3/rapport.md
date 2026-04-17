# Rapport – TP3 : Gestion de la latence

## Implémentation

**Prédiction des cercles**

`CircleGhost` prédit la position des cercles en simulant `localTick - serverTick` ticks en avance depuis la dernière position connue du serveur. L'offset par tick est préféré au RTT car il est déterministe et directement aligné sur les données reçues, indépendamment de l'horloge réseau. La simulation (`SimulateCircleTick`) reproduit exactement la physique de `MovingCircle` : déplacement par vélocité et réflexion aux bords avec `Time.fixedDeltaTime`.

**Prédiction d'inputs et réconciliation**

Chaque input WASD est appliqué immédiatement à `m_PredictedPosition` et conservé dans `m_InputHistory`. À chaque confirmation de tick serveur (`m_ServerConfirmedTick`), l'historique est purgé jusqu'au tick confirmé et les inputs restants sont rejoués sur la position autoritaire du serveur. Une simple téléportation vers la position serveur aurait produit des corrections visibles à chaque RTT en l'absence d'input adverse.

**Prédiction du stun**

`GameState.IsStunned` retourne `m_IsStunned.Value || m_IsLocallyStunned`. Quand le client appuie sur espace, `m_IsLocallyStunned` est activé immédiatement, puis effacé à réception de la confirmation serveur. Tous les systèmes existants honorent ainsi la prédiction sans modification.

**Artefacts visuels du stun**

Au début du stun, `CircleGhost` capture `m_FrozenPosition` depuis `transform.localPosition` (position prédite courante) plutôt que depuis `m_MovingCircle.Position` (position serveur en retard), ce qui évite un recul visuel. À la fin du stun, `m_ServerTick` est artificiellement gonflé de `stun_duration` ticks puisqu'il était gelé côté serveur pendant le stun. Pour compenser, `ticksToPredict` repart de 0 et croît progressivement jusqu'à `preStunTicks` via `localTick - m_StunEndLocalTick`, éliminant la téléportation avant en fin de stun.

## Maintenabilité

La duplication de logique de simulation entre `MovingCircle`/`CircleGhost` et les chemins serveur/client de `Player` constitue le principal risque : toute modification physique doit être répercutée manuellement des deux côtés. Centraliser ces fonctions en méthodes statiques partagées réduirait ce couplage implicite, au coût d'une légère complexification structurelle.
