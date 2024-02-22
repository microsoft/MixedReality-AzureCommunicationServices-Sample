using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.SpatialManipulation;

/// <summary>
/// This class adds some custom logic to toggle the pin buttons under special circumstances.
/// When another pin button controlling the same UI has been toggled or the user goes a certain distance from the UI.
/// </summary>
public class CustomPinButtonLogic : MonoBehaviour
{
    [Tooltip("The solver being controlled.")]
    [SerializeField]
    private Solver solver;

    [Tooltip("The SolverHandler, used to get the target transform")]
    [SerializeField]
    private SolverHandler solverHandler;

    [Tooltip("The toggle buttons which control this solver.")]
    [SerializeField]
    private List<PressableButton> pinToggles = new List<PressableButton>();

    [Tooltip("The distance the user can travel from the UI before the button is unpinned.")]
    [SerializeField]
    private float maxDistance = 3.0f;

    private float squareMaxDistance;
    private UnityAction<float> onEnteredListener;
    private UnityAction<float> onExitedListener;

    /// A Unity event function that is called on the frame when a script is enabled just before any of the update methods.
    void Start()
    {
        onEnteredListener = (_) => SolverToggle(false);
        onExitedListener = (_) => SolverToggle(true);

        foreach (PressableButton button in pinToggles)
        {
            button.IsToggled.OnEntered.AddListener(onEnteredListener);
            button.IsToggled.OnExited.AddListener(onExitedListener);
        }
        squareMaxDistance = maxDistance * maxDistance;
    }

    void OnDestroy()
    {
        foreach (PressableButton button in pinToggles)
        {
            button.IsToggled.OnEntered.RemoveListener(onEnteredListener);
            button.IsToggled.OnExited.RemoveListener(onExitedListener);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // If the UI is more than the max distance from the user, unpin it to enable the solver
        if (squareMaxDistance < (solver.transform.position - solverHandler.TransformTarget.position).sqrMagnitude)
        {
            SolverToggle(true);
        }
    }

    private void SolverToggle(bool solverEnabled)
    {
        solver.enabled = solverEnabled;
        ToggleOtherButtons();
    }

    private void ToggleOtherButtons()
    {
        // Ensure that all of the buttons reflect this change 
        foreach (PressableButton button in pinToggles)
        {
            // The button being toggled and the solver being enabled should be inverse
            if (button.IsToggled == solver.enabled)
            {
                button.ForceSetToggled(!solver.enabled);
            };
        }
    }
}
