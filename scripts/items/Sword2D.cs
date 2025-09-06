#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using PlantProject.Core;

namespace PlantProject.Items
{
    [GlobalClass]
    public partial class Sword2D : Node2D, IWeapon2D
    {
        [Export] public string ItemName { get; set; } = "Sword";
        [Export] public float AttackDuration { get; set; } = 0.12f;
        [Export] public float AttackCooldown { get; set; } = 0.25f;
        [Export] public int ZIndexSword { get; set; } = 200;
        [Export] public int BaseDamage { get; set; } = 10;
        [Export] public float ConeHalfAngleDegrees { get; set; } = 60f; // half-angle of the arc
        [Export] public float RangePadding { get; set; } = 12f; // small tolerance at tip/start

        private Sprite2D? _sprite;
        private Vector2 _facing = Vector2.Right;
        private bool _isAttacking;
        private float _attackTimer;
        private float _cooldownTimer;
        private readonly HashSet<IUnit> _hitThisSwing = new();

        public override void _Ready()
        {
            var swordTex = GD.Load<Texture2D>("res://assets/player/sword.svg");
            if (swordTex != null)
            {
                _sprite = new Sprite2D
                {
                    Name = "Sprite",
                    Texture = swordTex,
                    Visible = false,
                    ZIndex = ZIndexSword
                };
                AddChild(_sprite);
            }
        }

        public override void _Process(double delta)
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= (float)delta;

            if (_isAttacking)
            {
                _attackTimer -= (float)delta;
                if (_attackTimer <= 0f)
                {
                    EndAttack();
                }
                else
                {
                    UpdateTransform();
                    DoHitCheck();
                }
            }
        }

        public bool RequestAttack(IUnit user, Vector2 facing)
        {
            if (_isAttacking || _cooldownTimer > 0f)
                return false;

            _facing = facing == Vector2.Zero ? Vector2.Right : facing.Normalized();
            _ownerUnit = user as GodotUnit2D;
            StartAttack();
            return true;
        }

        private GodotUnit2D? _ownerUnit;

        private void StartAttack()
        {
            _isAttacking = true;
            _attackTimer = AttackDuration;
            _cooldownTimer = AttackCooldown;
            _hitThisSwing.Clear();
            UpdateTransform();
            if (_sprite != null) _sprite.Visible = true;

            // Fire the first action on the owning unit to emit events, if present
            if (GetParent() is GodotUnit2D unit && unit.Actions.Count > 0)
            {
                var action = unit.Actions[0];
                if (unit.CanPerformAction(action))
                    unit.PerformAction(action);
            }
        }

        private void EndAttack()
        {
            _isAttacking = false;
            if (_sprite != null) _sprite.Visible = false;
        }

        private void UpdateTransform()
        {
            if (_sprite == null)
                return;

            var dir = _facing;
            Rotation = dir.Angle();

            // Offset from parent sprite extent so the sword pokes out
            float parentExtent = 0f;
            if (GetParent() is Node parent)
            {
                var pSprite = parent.GetNodeOrNull<Sprite2D>("Sprite");
                if (pSprite?.Texture != null)
                {
                    var texSize = pSprite.Texture.GetSize();
                    var scale = pSprite.Scale;
                    parentExtent = Mathf.Max(texSize.X * scale.X, texSize.Y * scale.Y) * 0.5f;
                }
            }

            var swordSize = _sprite.Texture?.GetSize() ?? new Vector2(64, 12);
            float swordHalfLen = swordSize.X * 0.5f * _sprite.Scale.X;
            float offset = parentExtent + swordHalfLen;
            Position = dir * offset;
        }

        private void DoHitCheck()
        {
            if (_ownerUnit == null) return;

            var user = _ownerUnit;
            if (!user.IsAlive) return;

            var dir = _facing.Normalized();
            var userPos = user.GlobalPosition;

            // Measure sword geometry for range; visuals remain a bar, but effect is a cone
            Vector2 swordSize = _sprite?.Texture?.GetSize() ?? new Vector2(64, 12);
            float swordLen = swordSize.X * (_sprite?.Scale.X ?? 1f);

            // Parent extent so swing starts outside user sprite
            float parentExtent = 0f;
            var pSprite = user.GetNodeOrNull<Sprite2D>("Sprite");
            if (pSprite?.Texture != null)
            {
                var texSize = pSprite.Texture.GetSize();
                var scale = pSprite.Scale;
                parentExtent = Mathf.Max(texSize.X * scale.X, texSize.Y * scale.Y) * 0.5f;
            }

            float inner = MathF.Max(0f, parentExtent - RangePadding);
            float outer = parentExtent + swordLen + RangePadding;
            float halfRad = Mathf.DegToRad(ConeHalfAngleDegrees);
            float cosHalf = MathF.Cos(halfRad);

            // Iterate all units via group
            foreach (var node in GetTree().GetNodesInGroup("units"))
            {
                if (node is not GodotUnit2D target) continue;
                if (target == user) continue;
                if (!target.IsAlive) continue;

                Vector2 toTarget = target.GlobalPosition - userPos;
                float dist = toTarget.Length();
                if (dist <= 0.0001f) continue;

                // Project along facing to ensure target is in front and within reach
                float forward = dir.Dot(toTarget); // signed distance along facing
                if (forward < inner || forward > outer) continue;

                // Angle check for cone spread
                Vector2 n = toTarget / dist;
                float dot = dir.Dot(n);
                if (dot >= cosHalf)
                {
                    if (_hitThisSwing.Add(target))
                    {
                        float effectivePower = MathF.Max(1f, user.PowerLevel * (1f + user.DamagePowerMod));
                        int damage = Math.Max(0, (int)MathF.Round(BaseDamage * effectivePower));
                        target.TakeDamage(damage, user);
                    }
                }
            }
        }
    }
}
