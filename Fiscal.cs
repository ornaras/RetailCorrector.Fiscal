﻿using System.Reflection;

namespace RetailCorrector;

public delegate void LoggingRecord
    (string text, bool isError = false, Exception? ex = null);

public abstract class Fiscal : IDisposable
{
    /// <summary>
    /// Наименование фискальной интеграции
    /// </summary>
    public string Name => AssemblyName.Name ?? "Неизвестная фискальная интеграция";
    /// <summary>
    /// Версия фискальной интеграции
    /// </summary>
    public Version Version => AssemblyName.Version ?? new Version(0, 0, 0);
    /// <summary>
    /// Формат фискального документа
    /// </summary>
    public FiscalFormat DocFormat => ffd;
    public static event LoggingRecord? Logging;

    private AssemblyName AssemblyName => GetType().Assembly.GetName();
    protected FiscalFormat ffd = FiscalFormat.Unknown;

    protected void Info(string text) => 
        Logging?.Invoke(text);
    protected void Error(string text, Exception? ex = null) => 
        Logging?.Invoke(text, true, ex);
    public bool CreateReceipt(Receipt info)
    {
        if (SessionStatus == SessionStatus.Unknown) return false;
        if (SessionStatus == SessionStatus.Expired && !CloseSession()) return false;
        if (SessionStatus == SessionStatus.Closed && !OpenSession()) return false;
        if (!OpenReceipt(info)) return CancelDocument();
        foreach (var pos in info.Items)
            if (!RegisterPosition(pos)) return CancelDocument();
        if (!Payment(info.Payment)) return CancelDocument();
        if (!CloseReceipt(info.RoundedSum)) return CancelDocument();
        return true;
    }

    protected bool Payment(Payment info)
    {
        var arr = (uint[])info;
        for (byte type = 0; type < 5; type++)
            if (arr[type] != 0 && !SinglePayment(type, arr[type])) return false;
        return true;
    }

    protected abstract SessionStatus SessionStatus { get; }
    public abstract bool IsConnected { get; }
    public abstract string LastErrorMessage { get; }
    public abstract int UnsendCounter { get; }

    protected virtual bool CancelDocument()
    {
        Info($"Отмена документа...");
        return false;
    }

    public abstract bool Configure(string args);
    public abstract bool Connect();
    protected abstract bool OpenSession();
    protected abstract bool OpenReceipt(Receipt info);
    protected abstract bool RegisterPosition(Position info);
    protected abstract bool SinglePayment(byte type, uint sum);
    protected abstract bool CloseReceipt(uint? totalSum);
    protected abstract bool CloseSession();
    public abstract bool Disconnect();
    public abstract void Dispose();
}
