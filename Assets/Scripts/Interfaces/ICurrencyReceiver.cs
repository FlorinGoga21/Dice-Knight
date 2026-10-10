using UnityEngine;

public interface ICurrencyReceiver
{
    bool CanReceiveCurrency { get; }
    Transform CurrencyTarget { get; }
    Sprite CurrencySprite { get; }

    void AddCurrency(int amount);
}
