namespace IL_Rewriting
{
    [NotifyPropertyChanged]
    public class Person
    {
        public string Name { get; set; }

        public string LastName { get; set; }

        public int Code { get; set; }

        public string Department { get; set; }

        public override string ToString()
        {
            return string.Format("Name: {0}, LastName: {1}, Code: {2}, Department: {3}", Name, LastName, Code, Department);
        }
    }
}
