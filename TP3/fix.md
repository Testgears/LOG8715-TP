# Fix – Stun prediction visual artifacts

## Symptômes observés

En appuyant sur espace (avec latence simulée ~250ms) :

1. **Téléport arrière** — la simulation saute légèrement en arrière au moment de l'appui
2. **Cercles continuent de bouger** — les boules blanches ignorent le stun local et avancent
3. **Carré s'arrête immédiatement** ✅ correct
4. **Tout s'arrête après ~500ms** ✅ correct (confirmation serveur arrive après RTT)
5. **Reprise normale après le stun** ✅ correct

## Cause racine — CircleGhost.cs uniquement

Deux symptômes, même cause. Quand le stun local s'active :

```csharp
Vector2 predictedPosition = m_MovingCircle.Position; // position SERVEUR (en retard ~RTT/2)
if (m_GameState.IsStunned)
{
    transform.localPosition = (Vector3)predictedPosition; // snap vers serveur → téléport arrière
    return;
}
```

- Avant le stun : cercles affichés en avance (prédiction tick-based)
- Activation stun local : snap à la position serveur (en retard) → **téléport arrière**
- Serveur pas encore au courant : `m_MovingCircle.Position` continue d'être mis à jour → **cercles semblent bouger**
- Le joueur est correct (`m_PredictedPosition` reste gelé, pas de snap)

## Fix à implémenter

**Fichier : `Assets/Scripts/GameEntities/CircleGhost.cs`**

Ajouter deux champs :
```csharp
private bool m_WasStunned = false;
private Vector2 m_FrozenPosition;
```

Remplacer le bloc stun dans `Update()` par :
```csharp
bool isStunned = m_GameState.IsStunned;

if (!m_WasStunned && isStunned)
    m_FrozenPosition = (Vector2)transform.localPosition; // capture la position prédite actuelle

m_WasStunned = isStunned;

if (isStunned)
{
    transform.localPosition = (Vector3)m_FrozenPosition; // gel sans snap, sans drift
    return;
}
```

## Vérification

Avec 250ms packet delay (ParrelSync) :
1. Appuyer espace → cercles gèlent sur place, aucun téléport arrière
2. Après ~500ms → serveur confirme, stun réel
3. Fin du stun → prédiction reprend normalement
