using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace StrikeSprite;

/// <summary>
/// Adds a visual under the struck creature whenever Ironclad resolves Strike.
///
/// The effect is parented to a dedicated <see cref="Node2D"/> root so a Spine
/// animation can be attached later without touching the trigger code.
/// </summary>
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad), "OnPlay")]
internal static class StrikeSpritePatch
{
    private static readonly List<Node2D> Spawned = new List<Node2D>();
    private static Texture2D? _texture;
    private static bool _textureResolved;

    [HarmonyPostfix]
    private static void Postfix(CardPlay cardPlay)
    {
        try
        {
            Spawn(cardPlay);
        }
        catch (Exception ex)
        {
            ModLog.Error($"spawn failed: {ex}");
        }
    }

    private static void Spawn(CardPlay? cardPlay)
    {
        Creature? target = cardPlay?.Target;
        if (target == null)
            return;

        NCombatRoom? room = NCombatRoom.Instance;
        if (room == null)
            return;

        NCreature? creatureNode = FindCreatureNode(room, target);
        if (creatureNode == null)
        {
            ModLog.Warn("target has no NCreature node (not in combat?)");
            return;
        }

        var root = new Node2D
        {
            Name = $"StrikeEffect_{Spawned.Count}",
            Position = new Vector2(ModConfig.OffsetX, ModConfig.OffsetY),
            ZIndex = ModConfig.ZIndex,
        };
        creatureNode.AddChild(root);

        var sprite = new Sprite2D
        {
            Name = "PlaceholderSprite",
            Texture = ResolveTexture(),
            Position = Vector2.Zero,
        };
        root.AddChild(sprite);

        // Spine hook: assets/spine/ is reserved for the animation; see its README.
        // When the files arrive, build the SpineSprite here and add it to `root`
        // instead of / alongside the placeholder sprite.
        AddSpineAnimation(root);

        Spawned.Add(root);

        ModLog.Debug(
            $"target_node={creatureNode.GetPath()} target_global={creatureNode.GlobalPosition} " +
            $"effect_global={root.GlobalPosition} z={root.ZIndex} " +
            $"texture={(_texture != null ? ModConfig.TextureFileName : "<none>")} total={Spawned.Count}");

        if (!ModConfig.KeepSpriteAlive)
            FreeLater(root);
    }

    private static NCreature? FindCreatureNode(NCombatRoom room, Creature target)
    {
        foreach (NCreature node in room.CreatureNodes)
        {
            if (node != null && GodotObject.IsInstanceValid(node) && node.Entity == target)
                return node;
        }

        return null;
    }

    private static Texture2D? ResolveTexture()
    {
        if (!_textureResolved)
        {
            _texture = TextureLoader.Load(ModConfig.TextureResPath, ModConfig.TextureFileName);
            _textureResolved = true;
        }

        return _texture;
    }

    /// <summary>Placeholder for the Spine animation; intentionally empty for now.</summary>
    private static void AddSpineAnimation(Node2D root)
    {
    }

    private static void FreeLater(Node2D node)
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
            return;

        var timer = tree.CreateTimer(ModConfig.LifetimeSeconds);
        timer.Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(node))
                node.QueueFree();
        };
    }
}
