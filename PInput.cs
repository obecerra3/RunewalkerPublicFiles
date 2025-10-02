// #define DEBUG_START_FLY
using UnityEngine;

public class PInput : InputEmitter {
    protected readonly UserKeyCodes userKeyCodes;

    public bool use_input;

    protected InputEmitter input_emitter;

    protected bool game_over = false;

    protected const float double_click_delay = 0.2f;
    protected float last_up_time;
    protected float last_down_time;
    protected float last_right_time;
    protected float last_left_time;

    public PInput() {
        userKeyCodes = UserKeyCodes.Instance;

        use_input = true;
    }

    public override void Start() {
        // Normal Start
        Player.Instance.p_rb.Freeze();

        Player.Instance.p_rb.debug_mode = false;

        Physics.gravity = new Vector3(0, 0, KGame.GRAVITY_FORCE);

        Player.Instance.gameObject.transform.position =
            new Vector3(390f, 270f, 0);

        Player.Instance.gameObject.transform.Translate(Vector3.back * 15);

        Player.Instance.p_rb.UnFreeze();

#if DEBUG_START_FLY
        SetInputEmitter(new FlyOverInputEmitter());
#endif // DEBUG_START_FLY
    }

    public override void Update() {
        if (game_over) {
            return;
        }

        if (!use_input && input_emitter != null) {
            // Need to call Update() on the input_emitter here.
            input_emitter.Update();
            // Populate values from input_emitter.
            horizontal_axis = input_emitter.horizontal_axis;
            vertical_axis = input_emitter.vertical_axis;
            run_key = input_emitter.run_key;
            jump_key_down = input_emitter.jump_key_down;
            right_click_key_down = input_emitter.right_click_key_down;
            left_click_key_down = input_emitter.left_click_key_down;
            zoom_key_down = input_emitter.zoom_key_down;
            mouse_pos = input_emitter.mouse_pos;
            special_key_down = input_emitter.special_key_down;
            special_key_up = input_emitter.special_key_up;
        } else if (use_input) {
            // Default behaviour is to populate PInput from Input
            horizontal_axis = Input.GetAxis("Horizontal");
            vertical_axis = Input.GetAxis("Vertical");
            run_key = Input.GetKey(userKeyCodes.Run);
            jump_key_down = Input.GetKeyDown(userKeyCodes.Jump);

            right_click_key_down = Input.GetMouseButtonDown(1);
            right_click_key_up = Input.GetMouseButtonUp(1);
            right_click_key = Input.GetMouseButton(1);

            left_click_key_down = Input.GetMouseButtonDown(0);
            left_click_key_up = Input.GetMouseButtonUp(0);
            left_click_key = Input.GetMouseButton(0);

            zoom_key_down = Input.GetKeyDown(userKeyCodes.Zoom);
            mouse_pos = Input.mousePosition;
            special_key_down = Input.GetKeyDown(userKeyCodes.Special);
            special_key_up = Input.GetKeyUp(userKeyCodes.Special);
            
            if ((Input.GetKeyDown("w") || Input.GetKeyDown("up")) && last_up_time > (Time.time - double_click_delay)) {
                double_click_up = true;
            } else if ((Input.GetKeyDown("w") || Input.GetKeyDown("up"))) {
                last_up_time = Time.time;
            } else {
                double_click_up = false;
            }

            if ((Input.GetKeyDown("s") || Input.GetKeyDown("down")) && last_down_time > (Time.time - double_click_delay)) {
                double_click_down = true;
            } else if ((Input.GetKeyDown("s") || Input.GetKeyDown("down"))) {
                last_down_time = Time.time;
            } else {
                double_click_down = false;
            }

            if ((Input.GetKeyDown("d") || Input.GetKeyDown("right")) && last_right_time > (Time.time - double_click_delay)) {
                double_click_right = true;
            } else if ((Input.GetKeyDown("d") || Input.GetKeyDown("right"))) {
                last_right_time = Time.time;
            } else {
                double_click_right = false;
            }

            if ((Input.GetKeyDown("a") || Input.GetKeyDown("left")) && last_left_time > (Time.time - double_click_delay)) {
                double_click_left = true;
            } else if ((Input.GetKeyDown("a") || Input.GetKeyDown("left"))) {
                last_left_time = Time.time;
            } else {
                double_click_left = false;
            }
        }
        // This will be true regardless of input_emitter used.
        dir_input_total = Mathf.Abs(horizontal_axis) + Mathf.Abs(vertical_axis);
        dir_input = (dir_input_total) > 0;

#if DEBUG
        // Fly.
        if (Input.GetKeyDown(KeyCode.F)) {
            if (use_input) {
                SetInputEmitter(new FlyOverInputEmitter());
            } else {
                SetUseInput();
            }
        }

        // Log the samples when L is pressed.
        if (Input.GetKeyDown(KeyCode.L)) {
            DebugProfiler.Instance.LogSamples();
        }

#endif // DEBUG
    }

    public void SetInputEmitter(InputEmitter input_emitter) {
        use_input = false;
        this.input_emitter = input_emitter;
        input_emitter.Start();
    }

    public void SetUseInput() {
        use_input = true;
        Start();
    }

    public void GameOver() {
        game_over = true;
    }

    public void Respawn() {
        game_over = false;
    }
}
