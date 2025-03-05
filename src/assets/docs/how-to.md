

# **Getting Started with MaIN**  

Welcome to MaIN! This guide will help you get started, explore key features, and make the most of the system.  

---

## **1. Install the CLI (Highly Recommended!)**  

Before you begin, it is **highly recommended** to download and install the **CLI tool**.  

🔹 **Why?**  
- It helps you **test** different setups quickly.  
- You can **validate ideas** before writing full applications.  
- It can **automatically set important environment variables** (like `modelPath`).  

📌 **To Install:** Check out the [CLI page](#/doc/cli) for full details on installation and setup.  

💡 **Can I start without the CLI?**  
Yes! If you prefer, you can skip to the next section, but you will need to manually configure some settings.  

---

## **2. Learn the Basics (Tutorial Section)**  

Familiarize yourself with MaIN by going through the [tutorial section](#/doc/tutorial).  

🔹 **Key Notes:**  
- If you **didn’t install the CLI** or **didn’t set the `modelPath`**, you’ll need to manually specify the model path using:  
  ```csharp
  chatContext.WithCustomModel("your-model", "/path/to/model");
  ```  
- You can also use one of the **integrations**, but most of them require an **API key** to connect.  

---

## **3. Explore Advanced Examples**  

Once you're comfortable with the basics, dive into the **examples section** to see more **complex scenarios** and best practices.  

🔹 **What you’ll find:**  
- Multi-agent workflows  
- File processing in chat sessions  
- Custom model integration  
- Interactive chat behaviors  

---

## **4. Experiment and Have Fun!**  

There are no boundaries — the only limit is your creativity!
💡 **Run your agent flow in real time:**  
If you've built a flow of agents, you can **test it live** using the **technological demo page**.  

🚀 **To launch the demo:**  
```sh
mcli start-demo
```  

Go ahead and explore—**the sky is the limit!** 🎉  

---

That’s it! You're now ready to **build, experiment, and innovate** with MaIN. 🚀