using MTVBAForm;

public class Entry{
[STAThread]
    public static void Main(string[] args){
        try{
            MTVBAView form = new MTVBAView();
            Application.Run(form);
        } catch (Exception e){
            MessageBox.Show(e.Message, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}