using Microsoft.Extensions.Configuration;
using DatabaseLayer;
using Serilog;
using SpeechTodoApp;
using System.Runtime.CompilerServices;

using var log = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

VoiceService vs = new VoiceService();
DatabaseLayer.DatabaseLayer db = new DatabaseLayer.DatabaseLayer();

while (true)
{
    Console.WriteLine("Things that I have to do!");
    Console.WriteLine("1-Add task");
    Console.WriteLine("2-View tasks");
    Console.WriteLine("3-Update task");
    Console.WriteLine("4-Delete task");
    Console.WriteLine("5-Delete ALL task");
    Console.WriteLine("0- Exit");

    int choice = 0;
    bool exit = false;

    while (!Int32.TryParse(Console.ReadLine(), out choice))
    {
        Console.WriteLine("Wrong choice, try again.");
    }

    switch (choice)
    {
        case 0:
            exit = true;
            break;
        case 1:
            Console.WriteLine("Please say aloud the Activity and the Date! Example: Gym on August 24th");
            bool validObject = false;
            while (validObject == false)
            {
                try
                {
                    TodoTask t = await BuildTask();
                    validObject = true;
                    log.Information($"Adding object {t.ToString()} ");
                    await db.InsertTodoTask(t);
                    break;
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Error recognizing speech. Try again.");
                    validObject = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Error recognizing speech. Try again.");
                    validObject = false;
                }
                 
            }
                break;
        case 2:
            IEnumerable<TodoTask> taskList = await db.GetAll();
            foreach(var task in taskList)
            {
                Console.WriteLine(task.ToString());
            }
            break;
        case 3:
            taskList = await db.GetAll();
            foreach (var task in taskList)
            {
                Console.WriteLine(task.ToString());
            }
            Console.WriteLine("Which task you want to edit? Type the number:");
            int indexChoice = 0;
            while(!Int32.TryParse(Console.ReadLine(), out indexChoice)){
                Console.WriteLine("Invalid Choice. Try Again.");
            }

            Console.WriteLine("Ok, tell me the new activity and/or date");
            validObject = false;
            while (validObject == false)
            {
                try
                {
                    TodoTask t = await BuildTask(indexChoice);
                    validObject = true;
                    log.Information($"Adding object {t.ToString()} ");
                    var updateSuccess = await db.UpdateTodoTask(t);
                    if (updateSuccess)
                    {
                        log.Information("Success in updating.");
                    }
                    else
                    {
                        log.Information("Error in updating record.");
                    }
                    break;
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Error recognizing speech. Try again.");
                    validObject = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Error recognizing speech. Try again.");
                    validObject = false;
                }

            }

            break;
            case 4:
            taskList = await db.GetAll();
            foreach (var task in taskList)
            {
                Console.WriteLine(task.ToString());
            }
            Console.WriteLine("Which task to delete? Type in the index");
            indexChoice = 0;
            while (!Int32.TryParse(Console.ReadLine(), out indexChoice))
            {
                Console.WriteLine("Invalid Choice. Try Again.");
            }
            TodoTask deleteTaskED = new TodoTask("none", null, indexChoice);
            var success = await db.DeleteTodoTask(deleteTaskED);
            if (success)
            {
                log.Information("Success in deletion.");
            }
            else
            {
                log.Information("Error deleting task.");
            }
            break;
            case 5:
            Console.WriteLine("We will delete everything. Are you sure? Y/N");
            bool validChoice = false;
            string strChoice = "";
            while (!validChoice && string.IsNullOrWhiteSpace(strChoice))
            {

                strChoice = Console.ReadLine().Trim().ToUpper();
                if (strChoice.Equals("Y") || strChoice.Equals("N")){
                    validChoice = true;
                    break;
                }
                Console.WriteLine("Wrong choice. Try again. Y for Yes, N for No.");
            }

            if (strChoice.Equals("Y"))
            {
                success = await db.DeleteAllTask();
                if (success)
                {
                    log.Information("Success in deleting ALL tasks");
                }
                else
                {
                    log.Information("Failure in deleting ALL tasks.");
                }
            }
            break;
        default: break;



    }

    if (exit)
    {
        break;
    }

}


async Task<TodoTask> BuildTask(int id = 0)
{
    var speechRecognitionRes = await vs.GetSpeechResult();
    string speechText = speechRecognitionRes.Text;
    var speechTextAnalysisDateTime = await vs.GetTextAnalysis(speechText);

    DateTime date = GetDateTime(speechText);
    if (id == 0)
    {
        TodoTask tdTask = new TodoTask(speechText, date);       
        return tdTask;

    }
    else
    {
        TodoTask tdTask = new TodoTask(speechText, date, id);
        return tdTask;

    }
    throw new Exception("Error building task. BuildTask()");
}


DateTime GetDateTime(string text)
{
    string[] months = {"Empty", "january", "february", "march", "april", "may", "june", "july", "august", "september", "october",
                       "november", "december"};

    string[] ordinalExpanded = { "Empty", "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eight", "ninth", "tenth" };

    string[] ordinal = { "Empty", "1st", "2nd", "3rd", "4th", "5th", "6th", "7th", "8th", "9th", "10th",
"11th", "12th", "13th", "14th", "15th", "16th", "17th", "18th", "19th",
"20th", "21st", "22nd", "23rd", "24th", "25th", "26th", "27th", "28th",
"29th", "30th", "31st" };

    var words = text.Split(new[] { ' ', '.', ',', '!', '?', ';', ':' });
    log.Information(words.ToString());

    int day = 0;
    int month = 0;
    int year = 0;

    //check days
    day = FindMatch(words, ordinalExpanded);

    if (day == 0)
    {
        day = FindMatch(words, ordinal);
    }

    if (day == 0)
    {
        foreach (var word in words)
        {
            int testDay = 0;
            if (Int32.TryParse(word, out testDay))
                if (testDay > 0 && testDay <= 31)
                {
                    day = testDay;
                    break;
                }
        }
    }
    //check month
    month = FindMatch(words, months);

    //check year
    year = ParseYear(words);

    if(day == 0 || month == 0)
    {
        throw new ArgumentException("Couldn't parse a date");
    }
    try
    {
        return new DateTime(year, month, day);
    } catch(ArgumentOutOfRangeException ex)
    {
        throw new ArgumentException("Invalid date");
    }
}

static int FindMatch(string[] words, string[] searchTerms)
{
    for (int i = 0; i < searchTerms.Length; i++)
    {
        if (words.Contains(searchTerms[i], StringComparer.OrdinalIgnoreCase))
        {
            return i ;
        }

    }
    return 0;
}

static int ParseYear(string[] words)
{
    int year = 0;

    foreach(var word in words)
    {
        if(Int32.TryParse(word, out year))
        {
            if(year>1900 && year < 2100)
            {
                return year;
            }
        }
    }
        return DateTime.Now.Year;


}

//DatabaseLayer.DatabaseLayer db = new DatabaseLayer.DatabaseLayer();
//bool deleteStatus = await db.DeleteAllTask();
//if( deleteStatus){
//    log.Information("Deleted all data.");
//   }

//TodoTask task = new TodoTask("Television", DateTime.Now.Date);
//log.Information("Insert into db " + task.ToString());
//Task<bool> success = db.InsertTodoTask(task);

//IEnumerable<TodoTask> tasks = await db.GetAll();
//foreach(var taskFromList in tasks) {
//    log.Information(taskFromList.ToString());
//}
// TodoTask taskUpdated= new TodoTask("We changed this", null);




//success = db.UpdateTodoTask(taskUpdated);


//bool successDelete = await db.DeleteTodoTask(task);






