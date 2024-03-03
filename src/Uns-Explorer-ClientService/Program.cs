using Uns.Explorer.Services;

var clientService = new ClientService();
clientService.Subscribe("#");
clientService.ValueUpdated += (p, v) =>
{
    //Console.WriteLine($"{p} = {v}");
};


Console.ReadLine();