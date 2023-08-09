namespace Entities;

public class DbInitializer
{
    public async static Task<bool> Init(AppDbContext context)
    {
        context.Database.EnsureCreated();
        if (context.PlayList.Any())
        {
            return false;
        }

        // Add default data
        context.PlayList.Add(new PlayListItem
        {
            Scope = "shop1",
            Start = DateTime.Now,
            End = DateTime.Now.AddDays(1),
            Url = "https://www.bing.com"
        }); 
        context.PlayList.Add(new PlayListItem
        {
            Scope = "shop2",
            Start = DateTime.Now.AddDays(-2),
            End = DateTime.Now.AddDays(-1),
            Url = "https://www.bing.com"
        });
        context.PlayList.Add(new PlayListItem
        {
            Scope = null,       // All shops
            Start = DateTime.Now,
            End = DateTime.Now.AddDays(1),
            Url = "https://www.bing.com"
        });

        await context.SaveChangesAsync();

        return true;
    }
}
