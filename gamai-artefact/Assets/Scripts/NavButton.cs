using System.Threading.Tasks;
using UnityEngine;

public class NavButton : MonoBehaviour
{
    [SerializeField] private RectTransform book;
    private static int page = 1;
    private int SPEED = 500;

    public async void Right()
    {
        if (page == 5) return;
        var destination = new Vector3(book.position.x - Screen.width, book.position.y, book.position.z);
        while (destination.x < book.position.x && Application.IsPlaying(this))
        {
            Debug.Log(destination + " " + book.position);
            book.position -= new Vector3(1, 0, 0) * Time.deltaTime * SPEED * Screen.width/100;
            await Task.Yield();
        }
        book.position = destination;
        page -= 1;
    }

    public async void Left()
    {
        if (page == 1) return;
        var destination = new Vector3(book.position.x + Screen.width, book.position.y, book.position.z);
        while (destination.x > book.position.x && Application.IsPlaying(this))
        {
            Debug.Log(destination + " " + book.position);
            book.position += new Vector3(1, 0, 0) * Time.deltaTime * SPEED * Screen.width/100;
            await Task.Yield();
        }
        book.position = destination;
        page += 1;
    }
}
