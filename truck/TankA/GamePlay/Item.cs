using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TankA.Network;

namespace TankA.GamePlay
{
    public struct ItemState
    {
        public ItemType type;
        public Vector2 position;
        public int waitingTime;
        public int activeTime;
        public byte affectedTankIndex;
        public int oldValue;
        public byte itemIndex;
    }

    public enum ItemCategory
    {
        TankSpeed,
        AmmoSpeed,
        Armor,
        TBF,
        Other
    }

    public enum ItemType
    {
        MaxTankSpeed,
        IncreaseTankSpeed,
        MinTankSpeed,
        MinimizeEnemiesTanksSpeed,

        MaxAmmoSpeed,
        IncreaseAmmoSpeed,
        MinAmmoSpeed,
        MinimizeEnemiesTanksAmmoSpeed,

        MaxArmor,
        IncreaseArmor,
        MinArmor,
        MinimizeEnemiesTanksArmor,

        MinTBF,
        DecreaseTBF,
        MaxTBF,
        MaximizeEnemiesTBF,

        Add50Health,
        MakeTankInvulnerable,
        ChangeAmmoToRocket,
        ChangeAmmoToFireBall,
        ChangeAmmoToPhiTieu,
        FreezeAllEnemiesTanks,
        Random
    }

    class Item : Sprite
    {
        // Constructors.
        static Item()
        {
            ItemState state = new ItemState()
            {
                type = ItemType.MaxTankSpeed,
                waitingTime = 10000,
                activeTime = 10000,
                affectedTankIndex = 0xff,
                itemIndex = 0xff,
            };
            defaultItemState.Add(state.type, state);
            state.type = ItemType.IncreaseTankSpeed;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.MinTankSpeed;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.MinimizeEnemiesTanksSpeed;
            defaultItemState.Add(state.type, state);

            state.type = ItemType.MaxAmmoSpeed;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.IncreaseAmmoSpeed;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.MinAmmoSpeed;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.MinimizeEnemiesTanksAmmoSpeed;
            defaultItemState.Add(state.type, state);

            state.type = ItemType.MaxArmor;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.IncreaseArmor;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.MinArmor;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.MinimizeEnemiesTanksArmor;
            defaultItemState.Add(state.type, state);

            state.type = ItemType.MinTBF;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.DecreaseTBF;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.MaxTBF;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.MaximizeEnemiesTBF;
            defaultItemState.Add(state.type, state);

            state.type = ItemType.Add50Health;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.MakeTankInvulnerable;
            state.activeTime = 5000;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.ChangeAmmoToRocket;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.ChangeAmmoToFireBall;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.ChangeAmmoToPhiTieu;
            defaultItemState.Add(state.type, state);
            state.type = ItemType.FreezeAllEnemiesTanks;
            defaultItemState.Add(state.type, state);
        }
        public Item(SpriteInfo si, ItemState state, SpriteDestroyedHandler handler)
            : base(si)
        {
            this.position = state.position;
            this.activeTime = state.activeTime;
            this.waitingTime = state.waitingTime;
            this.type = state.type;
            this.hasBeenDestroyed = handler;
            this.affectedTankIndex = state.affectedTankIndex;
            this.oldValue = state.oldValue;
            this.itemIndex = state.itemIndex;

            if (waitingTime <= 0)
                activated = true;

            occupiedArea.X = (int)position.X;
            occupiedArea.Y = (int)position.Y;
            occupiedArea.Width = FrameWidth;
            occupiedArea.Height = FrameHeight;
        }
        public static ItemState DefaultItemState(ItemType type)
        {
            return defaultItemState[type];
        }
        // Update and draw.
        public void Update(GameTime gameTime, Dictionary<byte, Tank> tanks)
        {
            base.Update(gameTime);
            if (activated)
            {
                activeTime -= gameTime.ElapsedGameTime.Milliseconds;
                if (activeTime <= 0)
                {
                    if (tanks.ContainsKey(affectedTankIndex))
                        Deactivate(tanks[affectedTankIndex]);

                    OnDestroyed();
                }
            }
            else
            {
                waitingTime -= gameTime.ElapsedGameTime.Milliseconds;
                if (waitingTime <= 0)
                {
                    OnDestroyed();
                }
                // Collision detection.
                if (NetworkManager.Role == GameLogicMode.Client)
                    return;
                foreach(Tank t in tanks.Values)
                    if (occupiedArea.Intersects(t.OccupiedArea))
                    {
                        Activate(tanks, t.TankIndex);

                        var host = (ITankAHost)TankAGame.ThisGame.Services.GetService(typeof(ITankAHost));
                        if (host != null)
                            host.AnnounceItemActivation(itemIndex, t.TankIndex);
                        break;
                    }
            }
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            if (activated)
            {
                if (needDisplayInfo)
                {
                    spriteBatch.Draw(this.Texture, position, SourceRectangle, Color.White,
                        0, Vector2.Zero, 0.6f, SpriteEffects.None, Map.Layer1);
                    spriteBatch.DrawString(Factory.ItemInfoFont(), activeTime.ToString(), stringPosition, Color.Black);
                }
                return;
            }
            spriteBatch.Draw(this.Texture, position, SourceRectangle, Color.White,
                0, Vector2.Zero, 1, SpriteEffects.None, Map.Layer1);
        }

        public void ForceActive(Dictionary<byte, Tank> tanks, byte tankIndex)
        {
            if (activated)
                return;
            Activate(tanks, tankIndex);
        }
        public void ForceDeactive(Dictionary<byte, Tank> tanks)
        {
            if (affectedTankIndex != 0xff && tanks.ContainsKey(affectedTankIndex))
                Deactivate(tanks[affectedTankIndex]);
        }
        
        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            hasBeenDestroyed(this);
        }
        // Private methods
        void Activate(Dictionary<byte, Tank> tanks, byte tIndex)
        {
            UserTank uT = tanks[tIndex] as UserTank;
            if (uT != null && uT.PlayerIndex == 0)
            {
                needDisplayInfo = true;
            }
            EffectState state = new EffectState()
            {
                attachedTankIndex = 0xff,
                endFrame = new Point(1, 0),
                lastingTime = 3000
            };
            switch (type)
            {
                case ItemType.MaxTankSpeed:
                    oldValue = tanks[tIndex].Speed;
                    tanks[tIndex].Speed = Tank.MaxTankSpeed;
                    GetInfoPositions(ItemCategory.TankSpeed, ref position, ref stringPosition);
                    break;
                case ItemType.IncreaseTankSpeed:
                    tanks[tIndex].Speed += 1;
                    GetInfoPositions(ItemCategory.TankSpeed, ref position, ref stringPosition);
                    state.type = EffectType.II_IncreaseTankSpeed;
                    state.position = position;
                    Factory.CreateEffect(state);
                    OnDestroyed();
                    break;
                case ItemType.MinTankSpeed:
                    oldValue = tanks[tIndex].Speed;
                    tanks[tIndex].Speed = Tank.MinTankSpeed;
                    GetInfoPositions(ItemCategory.TankSpeed, ref position, ref stringPosition);
                    break;
                case ItemType.MinimizeEnemiesTanksSpeed:
                    ActivateUponEnemiesTanks(tanks, tanks[tIndex].Team, ItemType.MinTankSpeed);
                    OnDestroyed();
                    break;

                case ItemType.MaxAmmoSpeed:
                    oldValue = tanks[tIndex].AmmoSpeed;
                    tanks[tIndex].AmmoSpeed = Tank.MaxAmmoSpeed;
                    GetInfoPositions(ItemCategory.AmmoSpeed, ref position, ref stringPosition);
                    break;
                case ItemType.IncreaseAmmoSpeed:
                    tanks[tIndex].AmmoSpeed += 1;
                    GetInfoPositions(ItemCategory.AmmoSpeed, ref position, ref stringPosition);
                    state.type = EffectType.II_IncreaseAmmoSpeed;
                    state.position = position;
                    Factory.CreateEffect(state);
                    OnDestroyed();
                    break;
                case ItemType.MinAmmoSpeed:
                    oldValue = tanks[tIndex].AmmoSpeed;
                    tanks[tIndex].AmmoSpeed = Tank.MinAmmoSpeed;
                    GetInfoPositions(ItemCategory.AmmoSpeed, ref position, ref stringPosition);
                    break;
                case ItemType.MinimizeEnemiesTanksAmmoSpeed:
                    ActivateUponEnemiesTanks(tanks, tanks[tIndex].Team, ItemType.MinAmmoSpeed);
                    OnDestroyed();
                    break;

                case ItemType.MaxArmor:
                    oldValue = tanks[tIndex].Armor;
                    tanks[tIndex].Armor = Tank.MaxArmor;
                    GetInfoPositions(ItemCategory.Armor, ref position, ref stringPosition);
                    break;
                case ItemType.IncreaseArmor:
                    tanks[tIndex].Armor += 5;
                    GetInfoPositions(ItemCategory.Armor, ref position, ref stringPosition);
                    state.type = EffectType.II_IncreaseArmor;
                    state.position = position;
                    Factory.CreateEffect(state);
                    OnDestroyed();
                    break;
                case ItemType.MinArmor:
                    oldValue = tanks[tIndex].Armor;
                    tanks[tIndex].Armor = Tank.MinArmor;
                    GetInfoPositions(ItemCategory.Armor, ref position, ref stringPosition);
                    break;
                case ItemType.MinimizeEnemiesTanksArmor:
                    ActivateUponEnemiesTanks(tanks, tanks[tIndex].Team, ItemType.MinArmor);
                    OnDestroyed();
                    break;

                case ItemType.MinTBF:
                    oldValue = tanks[tIndex].TimeBetweenFires;
                    tanks[tIndex].TimeBetweenFires = Tank.MinTBF;
                    GetInfoPositions(ItemCategory.TBF, ref position, ref stringPosition);
                    break;
                case ItemType.DecreaseTBF:
                    tanks[tIndex].TimeBetweenFires -= 100;
                    GetInfoPositions(ItemCategory.TBF, ref position, ref stringPosition);
                    state.type = EffectType.II_DecreaseTBF;
                    state.position = position;
                    Factory.CreateEffect(state);
                    OnDestroyed();
                    break;
                case ItemType.MaxTBF:
                    oldValue = tanks[tIndex].TimeBetweenFires;
                    tanks[tIndex].TimeBetweenFires = Tank.MaxTBF;
                    GetInfoPositions(ItemCategory.TBF, ref position, ref stringPosition);
                    break;
                case ItemType.MaximizeEnemiesTBF:
                    ActivateUponEnemiesTanks(tanks, tanks[tIndex].Team, ItemType.MaxTBF);
                    OnDestroyed();
                    break;

                case ItemType.Add50Health:
                    tanks[tIndex].Health += 50;
                    OnDestroyed();
                    break;
                case ItemType.MakeTankInvulnerable:
                    tanks[tIndex].InvulnerableTime = activeTime;
                    OnDestroyed();
                    break;
                case ItemType.ChangeAmmoToRocket:
                    tanks[tIndex].AmmoType = AmmoType.Rocket;
                    OnDestroyed();
                    break;
                case ItemType.ChangeAmmoToFireBall:
                    tanks[tIndex].AmmoType = AmmoType.FireBall;
                    OnDestroyed();
                    break;
                case ItemType.ChangeAmmoToPhiTieu:
                    tanks[tIndex].AmmoType = AmmoType.PhiTieu;
                    OnDestroyed();
                    break;
                case ItemType.FreezeAllEnemiesTanks:
                    foreach (var t in tanks.Values)
                        if (t.Team != tanks[tIndex].Team)
                            t.FreezeTime = activeTime;
                    OnDestroyed();
                    break;
            }
            affectedTankIndex = tIndex;
            activated = true;
            waitingTime = 0;
        }
        void Deactivate(Tank t)
        {
            switch (type)
            {
                case ItemType.MaxTankSpeed:
                    t.Speed = (byte)oldValue;
                    break;
                case ItemType.MinTankSpeed:
                    t.Speed = (byte)oldValue;
                    break;
                
                case ItemType.MaxAmmoSpeed:
                    t.AmmoSpeed = (byte)oldValue;
                    break;
                case ItemType.MinAmmoSpeed:
                    t.AmmoSpeed = (byte)oldValue;
                    break;

                case ItemType.MaxArmor:
                    t.Armor = (byte)oldValue;
                    break;
                case ItemType.MinArmor:
                    t.Armor = (byte)oldValue;
                    break;

                case ItemType.MinTBF:
                    t.TimeBetweenFires = oldValue;
                    break;
                case ItemType.MaxTBF:
                    t.TimeBetweenFires = oldValue;
                    break;
            }
        }
        void ActivateUponEnemiesTanks(Dictionary<byte, Tank> tanks, byte myTeam, ItemType itType)
        {
            var map = (IMap)TankAGame.ThisGame.Services.GetService(typeof(IMap));
            foreach (var tank in tanks.Values)
                if (tank.Team != myTeam)
                {
                    ItemState state = new ItemState()
                    {
                        type = itType,
                        activeTime = activeTime,
                        waitingTime = 1,
                    };

                    Factory.CreateItem(state, map.AllItemsInMap()).ForceActive(tanks, tank.TankIndex);
                }
        }
        // Private methods.
        static void GetInfoPositions(ItemCategory cat, ref Vector2 iconPosition, ref Vector2 stringPosition)
        {
            iconPosition.X = 800;
            stringPosition.X = 850;
            switch (cat)
            {
                case ItemCategory.TankSpeed:
                    iconPosition.Y = 20;
                    stringPosition.Y = 30;
                    break;
                case ItemCategory.AmmoSpeed:
                    iconPosition.Y = 70;
                    stringPosition.Y = 80;
                    break;
                case ItemCategory.Armor:
                    iconPosition.Y = 120;
                    stringPosition.Y = 130;
                    break;
                case ItemCategory.TBF:
                    iconPosition.Y = 170;
                    stringPosition.Y = 180;
                    break;
                case ItemCategory.Other:
                    iconPosition.Y = 220;
                    stringPosition.Y = 230;
                    break;
            }

        }
        // Private members.
        Vector2 position;
        readonly Rectangle occupiedArea;
        int activeTime;
        int waitingTime;
        bool activated;
        int oldValue;
        byte affectedTankIndex;
        readonly ItemType type;
        readonly byte itemIndex;
        bool needDisplayInfo;
        Vector2 stringPosition;

        SpriteDestroyedHandler hasBeenDestroyed;

        static Dictionary<ItemType, ItemState> defaultItemState = new Dictionary<ItemType, ItemState>();
        // Properties.
        public byte ItemIndex
        {
            get { return itemIndex; }
        }
        public ItemType Type
        {
            get { return type; }
        }
        public ItemState State
        {
            get
            {
                ItemState ret = new ItemState()
                {
                    type = type,
                    position = position,
                    waitingTime = waitingTime,
                    activeTime = activeTime,
                    affectedTankIndex = affectedTankIndex,
                    oldValue = oldValue,
                    itemIndex = itemIndex,
                };
                return ret;
            }
        }
    }
}
