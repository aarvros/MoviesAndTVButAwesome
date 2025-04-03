using MTVBAForm;

public class Entry{
[STAThread]
    public static void Main(string[] args){
        try{
            if (args.Length > 0){
                string filePath = args[0];
                if (File.Exists(filePath) && Path.GetExtension(filePath).ToLower() == ".mp4"){
                    MTVBAView form = new MTVBAView(filePath);
                    Application.Run(form);
                }
            }else{
                MessageBox.Show("You must sepcify an mp4 file with this program!", "Missing File Arg", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        } catch (Exception e){
            MessageBox.Show(e.Message, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}