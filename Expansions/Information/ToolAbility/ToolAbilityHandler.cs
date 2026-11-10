using Localization;
using Player;
using UnityEngine;
using XpExpansions.Manager;

namespace Expansions.Information.ToolAbility
{
    public class ToolAbilityHandler : MonoBehaviour
    {
        public static ToolAbilityHandler Instance { get; private set; } = null!;
        public static bool Exists { get; private set; } = false;
        public static bool MovingMine => Exists && Instance._movingMine != null;
        public static bool InMineMode => Exists && Instance.DoMineUpdate;
        public static bool InSentryMode => Exists && Instance.DoSentryUpdate;
        private static PUI_InteractionPrompt s_interactPrompt = null!;
        public static bool CheckCanPlaceIgnoreHidden(MineDeployerFirstPerson deployer)
        {
            var fpHolder = deployer.FPItemHolder;
            bool hidden = fpHolder.ItemHiddenTrigger;
            fpHolder.ItemHiddenTrigger = false;
            bool result = deployer.CheckCanPlace();
            fpHolder.ItemHiddenTrigger = hidden;
            return result;
        }

        enum ToolMode
        {
            None,
            Sentry,
            Mine
        }

        private PlayerAgent _player = null!;
        private readonly CellSoundPlayer _sound = new();
        private ProgressGradient _slimGradient = null!;
        private ProgressGradient _selectedGradient = null!;

        private float _recallDuration = 1f;
        private float _recallDeltaMod = 0;
        private bool _recallingTool = false;
        private float _toolRecallProgress = 0;
        public float ToolRecallProgress
        {
            get => _toolRecallProgress;
            set
            {
                _toolRecallProgress = value;
                _slimGradient.Update(value);
                _selectedGradient.Update(value);
                if (_toolRecallProgress == 0)
                    s_interactPrompt.SetVisible(false);
            }
        }

        private Interact_Timed? _activeInteract;
        private ToolMode _mode = ToolMode.None;

        private SentryGunInstance? _sentry;

        private MineDeployerInstance? _movingMine;
        private MineDeployerFirstPerson? _mineDeployer;
        private Color _origMineColor = Color.black;
        private readonly Dictionary<IntPtr, MineDeployerInstance> _activeMines = new();

        public ToolAbilityHandler(IntPtr ptr) : base(ptr) { }

        private void Awake()
        {
            if (s_interactPrompt == null)
            {
                s_interactPrompt = GuiManager.InteractionLayer.AddRectComp("Gui/Player/PUI_InteractionPrompt_CellUI", GuiAnchor.MidCenter, new Vector2(0.0f, 0.0f), null).Cast<PUI_InteractionPrompt>();
                // Disable bar
                s_interactPrompt.transform.localPosition = new Vector3(0, -360, 0);
                s_interactPrompt.transform.GetChild(2).gameObject.SetActive(false);
                s_interactPrompt.transform.GetChild(3).gameObject.SetActive(false);
                // -----------
                s_interactPrompt.SetVisible(false);
            }

            _player = GetComponent<PlayerAgent>();
            Instance = this;
            Exists = true;

            var classSlot = GuiManager.PlayerLayer.Inventory.m_inventorySlots[InventorySlot.GearClass];
            SpriteRenderer slim = classSlot.m_slim_root.transform.GetChild(0).GetChild(5).GetComponent<SpriteRenderer>();
            SpriteRenderer selected = classSlot.m_selected_root.transform.GetChild(0).GetChild(1).GetComponent<SpriteRenderer>();
            _slimGradient = new(slim);
            _selectedGradient = new(selected);
        }

        private bool DoSentryUpdate => _mode == ToolMode.Sentry && ToolAbilityManager.ActiveAbility.SentryRecall;
        private bool DoMineUpdate
        {
            get
            {
                var ability = ToolAbilityManager.ActiveAbility;
                return _mode == ToolMode.Mine && _activeMines.Count > 0 && (ability.MineRecall || ability.MoveMines);
            }
        }

        private void Update()
        {
            if (DoSentryUpdate)
            {
                UpdateSentry();
            }
            else if (DoMineUpdate)
            {
                UpdateMines();
            }

            if (_recallingTool)
                ToolRecallProgress = Math.Min(1f, ToolRecallProgress + Clock.Delta * _recallDeltaMod);
        }

        private void UpdateSentry()
        {
            CheckToggleRecall(Text.Get(1419u), InputAction.SelectTool);

            if (ToolRecallProgress == 1)
            {
                StopRecall(true);
                ToolAbilityManager.DoSentryPickup(_player, _sentry!);
            }
        }

        private void UpdateMines()
        {
            if (_mineDeployer == null)
            {
                OnDeployerDespawn();
                return;
            }

            bool deployerHeld = _mineDeployer!.isActiveAndEnabled;
            if (ToolAbilityManager.ActiveAbility.MineRecall)
            {
                if (deployerHeld)
                {
                    if (_recallingTool)
                        s_interactPrompt.SetVisible(true);
                    CheckToggleRecall(Text.Get(1420u), InputAction.Reload);
                }
                else
                    s_interactPrompt.SetVisible(false);
            }

            if (_recallingTool && ToolRecallProgress == 1)
            {
                StopRecall(true);
                foreach (var mine in _activeMines.Values)
                    mine.WantItemAction(mine.Owner, SyncedItemAction_New.PickUp);
            }

            if (ToolAbilityManager.ActiveAbility.MoveMines)
            {
                if (_movingMine != null)
                {
                    if (!deployerHeld || !InputMapper.GetButtonKeyMouse(InputAction.Fire, _player.InputFilter))
                    {
                        var interact = _player.Interaction;
                        var ladder = interact.m_enterLadderVisible;
                        var selected = interact.m_bestSelectedInteract;
                        interact.m_enterLadderVisible = false;
                        interact.m_bestSelectedInteract = null;
                        if (CheckCanPlaceIgnoreHidden(_mineDeployer))
                        {
                            ToolAbilityManager.DoMineMove(_movingMine, _mineDeployer.m_lastRayHit.point, Quaternion.LookRotation(_mineDeployer.m_lastRayHit.normal * -1, _mineDeployer.Owner.transform.forward));
                        }
                        interact.m_enterLadderVisible = ladder;
                        interact.m_bestSelectedInteract = selected;
                        _mineDeployer.m_placementIndicator.m_colorEnabled = _origMineColor;
                        _mineDeployer.m_placementIndicator.m_cookieLight.color = _origMineColor;
                        _movingMine = null;
                    }
                }
                else if (deployerHeld && InputMapper.GetButtonDownKeyMouse(InputAction.Fire, _player.InputFilter))
                {
                    var interact = _activeInteract?.TryCast<MonoBehaviour>();
                    if (interact != null)
                    {
                        _movingMine = interact.transform.parent.GetComponent<MineDeployerInstance>();
                        _mineDeployer.m_placementIndicator.m_colorEnabled = Color.cyan;
                    }
                }
            }
        }

        private void CheckToggleRecall(string toolName, InputAction toggleAction)
        {
            if (InputMapper.GetButtonDownKeyMouse(toggleAction, _player.InputFilter))
            {
                if (!_recallingTool)
                {
                    _recallingTool = true;
                    _sound.Post(AK.EVENTS.INTERACT_TOOL_START);
                    s_interactPrompt.SetVisible(true);
                    s_interactPrompt.SetPrompt($"Cancel {toolName} Recall", Text.Format(825u, InputMapper.GetBindingName(toggleAction)));
                    if (_mode == ToolMode.Sentry)
                        _recallDeltaMod = ToolAbilityManager.ActiveAbility.SentryRecallSpeed / _recallDuration;
                    else
                        _recallDeltaMod = ToolAbilityManager.ActiveAbility.MineRecallSpeed / _recallDuration;
                }
                else
                    StopRecall();
            }
        }

        private void StopRecall(bool finished = false)
        {
            if (_recallingTool)
            {
                _recallingTool = false;
                _sound.Post(finished ? AK.EVENTS.INTERACT_TOOL_FINISHED : AK.EVENTS.INTERACT_TOOL_CANCEL);
                ToolRecallProgress = 0;
                s_interactPrompt.SetVisible(false);
            }
        }

        internal void OnSentrySpawn(SentryGunInstance sentry)
        {
            _mode = ToolMode.Sentry;
            _sentry = sentry;
            _activeInteract = sentry.PickupInteraction.Cast<Interact_Timed>();
            _recallDuration = _activeInteract.InteractDuration;
        }

        internal void OnSentryDespawn()
        {
            _mode = ToolMode.None;
            _sentry = null;
            _activeInteract = null;
            StopRecall();
        }

        internal void OnDeployerWield(MineDeployerFirstPerson deployer)
        {
            if (_mineDeployer != null && _mineDeployer.Pointer == deployer.Pointer) return;

            _mode = ToolMode.Mine;
            _mineDeployer = deployer;
            _origMineColor = deployer.m_placementIndicator.m_colorEnabled;
        }

        internal void OnDeployerDespawn()
        {
            if (_mineDeployer != null)
                _mineDeployer.m_placementIndicator.m_colorEnabled = _origMineColor;
            _mineDeployer = null;
            _activeInteract = null;
            StopRecall();
            _activeMines.Clear();
        }

        internal void OnMineSpawn(MineDeployerInstance instance)
        {
            var interact = instance.PickupInteraction.Cast<Interact_Timed>();
            interact.OnInteractionSelected += (Action<PlayerAgent, bool>)((player, selected) =>
            {
                if (selected)
                    _activeInteract = interact;
                else
                    _activeInteract = null;
            });
            _recallDuration = interact.InteractDuration;
            _activeMines.TryAdd(instance.Pointer, instance);
        }

        internal void OnMineDespawn(MineDeployerInstance mine)
        {
            if (_activeMines.Remove(mine.Pointer) && _activeMines.Count == 0)
            {
                _activeInteract = null;
                StopRecall();
            }
        }

        private void OnDestroy()
        {
            OnSentryDespawn();
            OnDeployerDespawn();
            _slimGradient.Destroy();
            _selectedGradient.Destroy();
            if (Instance == this)
            {
                Exists = false;
                Instance = null!;
            }
        }

        class ProgressGradient
        {
            private const float WidthBuffer = 50;
            private static Sprite s_sprite = null!;
            private static Sprite Sprite
            {
                get
                {
                    if (s_sprite != null)
                        return s_sprite;

                    Texture2D tex = new(1, 1);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();

                    s_sprite = Sprite.Create(
                        tex,
                        new Rect(0, 0, 1, 1),
                        new Vector2(0.5f, 0.5f),
                        1
                    );
                    return s_sprite;
                }
            }

            private readonly GameObject _overlayGO;
            private readonly GameObject _maskGO;
            private readonly (float x, float y) _size;

            public ProgressGradient(SpriteRenderer orig)
            {
                _overlayGO = new("ProgressOverlay");
                _overlayGO.transform.SetParent(orig.transform, false);
                _overlayGO.layer = LayerManager.LAYER_UI;
                var overlay = _overlayGO.AddComponent<SpriteRenderer>();

                var color = orig.color;
                color.g = 1f;
                color.a = 0.2f;
                overlay.sprite = orig.sprite;
                overlay.color = color;
                overlay.sortingLayerID = orig.sortingLayerID;
                overlay.sortingOrder = orig.sortingOrder + 1;
                overlay.drawMode = orig.drawMode;
                overlay.tileMode = orig.tileMode;
                overlay.spriteSortPoint = orig.spriteSortPoint;
                _size = (orig.size.x + WidthBuffer, orig.size.y);
                overlay.size = new(_size.x, _size.y);
                overlay.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

                _maskGO = new("ProgressMask");
                _maskGO.transform.SetParent(orig.transform, false);
                _maskGO.layer = LayerManager.LAYER_UI;
                var mask = _maskGO.AddComponent<SpriteMask>();
                mask.sprite = Sprite;
                mask.alphaCutoff = 0f;
                mask.frontSortingLayerID = orig.sortingLayerID;
                mask.backSortingLayerID = orig.sortingLayerID;
                mask.frontSortingOrder = orig.sortingOrder + 1;
                mask.backSortingOrder = orig.sortingOrder + 1;
            }

            public void Update(float progress)
            {
                _maskGO.transform.localPosition = new Vector3(_size.x * (progress * 0.5f - 1), 0, 0);
                _maskGO.transform.localScale = new Vector3(_size.x * progress, _size.y, 1);
            }

            public void Destroy()
            {
                GameObject.Destroy(_maskGO);
                GameObject.Destroy(_overlayGO);
            }
        }
    }
}
