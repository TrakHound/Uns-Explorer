namespace Uns.Explorer.Services
{
    public class ConnectionListService
    {
        private string _selectedConnectionId;

        public string SelectedConnectionId => _selectedConnectionId;

        public event EventHandler<string> ConnectionSelected;


        public void SelectConnection(string connectionId)
        {
            if (!string.IsNullOrEmpty(connectionId))
            {
                _selectedConnectionId = connectionId;

                if (ConnectionSelected != null) ConnectionSelected.Invoke(this, connectionId);
            }
        }
    }
}
