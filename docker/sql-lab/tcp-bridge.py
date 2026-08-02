import asyncio


async def pipe(reader: asyncio.StreamReader, writer: asyncio.StreamWriter) -> None:
    try:
        while data := await reader.read(65536):
            writer.write(data)
            await writer.drain()
    finally:
        writer.close()
        await writer.wait_closed()


async def forward(client_reader: asyncio.StreamReader, client_writer: asyncio.StreamWriter) -> None:
    try:
        server_reader, server_writer = await asyncio.open_connection("sql-lab", 1433)
    except OSError:
        client_writer.close()
        await client_writer.wait_closed()
        return

    await asyncio.gather(
        pipe(client_reader, server_writer),
        pipe(server_reader, client_writer),
        return_exceptions=True,
    )


async def main() -> None:
    server = await asyncio.start_server(forward, "0.0.0.0", 14333)
    async with server:
        await server.serve_forever()


asyncio.run(main())
