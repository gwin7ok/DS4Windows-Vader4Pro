using System.Collections.Generic;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels.Util;

namespace DS4WinWPF.DS4Forms.ViewModels.SpecialActions;

public class SpecialActionViewModel : NotifyDataErrorBase
{
    private double holdInterval;
    public double HoldInterval { get => holdInterval; set => holdInterval = value; }
    private int actionIndex;
    // Phase6-Step10-3-2: Global.SaveAction の置換先（必須引数）
    private readonly ISpecialActionRepository specialActionRepository;

    public SpecialActionViewModel(int actionIndex, ISpecialActionRepository specialActionRepository)
    {
        this.specialActionRepository = specialActionRepository ?? throw new System.ArgumentNullException(nameof(specialActionRepository));
        this.actionIndex = actionIndex;
    }

    public void LoadAction(SpecialAction action)
    {
        holdInterval = action.delayTime;
    }

    public void SaveAction(SpecialAction action, bool edit = false)
    {
        specialActionRepository.SaveAction(action.name, action.controls, actionIndex, $"{holdInterval.ToString("#.##", Global.configFileDecimalCulture)}", edit, holdInterval);
    }

    public override bool IsValid(SpecialAction action)
    {
        ClearOldErrors();

        bool valid = true;
        List<string> holdIntervalErrors = new List<string>();

        if (holdInterval < 0 || holdInterval > 60)
        {
            holdIntervalErrors.Add("Interval not valid");
            errors["HoldInterval"] = holdIntervalErrors;
            RaiseErrorsChanged("HoldInterval");
        }

        return valid;
    }

    public override void ClearOldErrors()
    {
        if (errors.Count > 0)
        {
            errors.Clear();
            RaiseErrorsChanged("HoldInterval");
        }
    }
}