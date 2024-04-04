import time
import os
from dotenv import load_dotenv
from openai import AzureOpenAI


# Load environment variables from .env file
load_dotenv()

client = AzureOpenAI(
    api_key=os.getenv("OPENAI_AZURE_KEY"),
    api_version="2024-02-01",
    azure_endpoint=os.getenv("OPENAI_AZURE_ENDPOINT")
)

# In this sample, we use key-based authentication. This is only done because this sample
# will be done by a larger group in a hackathon event. In real world, AVOID key-based
# authentication. ALWAYS prefer Microsoft Entra-based authentication (Managed Identity)!

# System prompt
system_prompt = """
You are an assistant that helps customer to find the right bike. Options are:

* Light, single-speed bike for urban commuting.
* Gravel bike designed to ride on many different surfaces.
* Cargo bike for transporting kids or goods.
* Racing bike for sports.
* Moutainbike designed for off-road cycling.
* All bike types above a also available with electric motors.

Ask the user about how she or he is going to use the bike. Make a suggestion
based on the intended use.

If transporting goods or kids seems to be important for the customer,
mention the option of using a bike trailer as an alternative for cargo bikes.
Point out that bike trailers should not be used with carbon bike frames.

Only answer questions related to bike type selection. If the user asks
questions not related to this topic, tell her or him that you cannot
answer such questions.
"""

# Initial assistant message to get the conversation started
assistant_message = "Hi! Can I help you find the right bike?"

print(f"🤖: {assistant_message}")

deploymentModel = os.getenv("OPENAI_AZURE_DEPLOYMENT")
while True:
    # Ask the user for a message. Exit program in case of empty message.
    user_message = input("\nYou (just press enter to exit the conversation): ")
    if not user_message:
        break

    # Send the messages to the API and wait for the response. Display a
    # waiting indicator while waiting for the response.
    print("\nThinking...", end="")

    response = client.chat.completions.create(model=deploymentModel,
                                              messages=[
                                                  {"role": "system",
                                                      "content": system_prompt},
                                                  {"role": "assistant",
                                                      "content": assistant_message},
                                                  {"role": "user",
                                                      "content": user_message}
                                              ])

    print("\n")
    if len(response.choices) == 0:
        print(f"Error:  no response from the API. Exiting...")
        break

    # Add the response from the API to the list of messages to send to the API
    assistant_message = response.choices[0].message.content
    print(f"🤖: {assistant_message}")
