namespace Persiltech.Blazor.JSInterop.Sample.Pages;

public partial class Add
{
    private int FirstAddend;
    private int SecondAddend;
    private string Message = string.Empty;

    [Inject]
    public AddWasmService Service { get; set; } = null!;

    private async Task Process()
    {
        var result = await Service.Add(FirstAddend, SecondAddend);

        Message = $"Result: {result}";
    }
}
