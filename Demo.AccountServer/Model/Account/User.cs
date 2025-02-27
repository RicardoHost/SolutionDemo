namespace Demo.AccountServer.Model.Account
{
    public class User
    {
        public string id { set; get; }
        public string name { set; get; }
        public int sex { get; set; }
        public int age { get; set; }

        public static User[] CreateExample()
        {
            return new User[] {
                new User {
                    id = Guid.NewGuid().ToString(),
                    name = "小明",
                    sex = 0,
                    age = 30
                },
                new User {
                    id = Guid.NewGuid().ToString(),
                    name = "小紅",
                    sex = 1,
                    age = 18
                },
                new User {
                    id = Guid.NewGuid().ToString(),
                    name = "小白",
                    sex = 1,
                    age = 17
                },
            };
        }
    }
}
