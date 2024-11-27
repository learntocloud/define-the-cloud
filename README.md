<div align="center">
  <h1 align="center"> define the cloud API </h1>
</div>

The API that powers [Define the Cloud](https://definethecloud.guide) homepage.

## Integrate into your own project

The URL [https://definethecloudapi-bwh8aehvcgdgg2fq.eastus2-01.azurewebsites.net/api/GetAllDefinitions](https://definethecloudapi-bwh8aehvcgdgg2fq.eastus2-01.azurewebsites.net/api/GetAllDefinitions) shows you 10 definitions per page by default. However, you can use the `pageSize` URL parameter to increase the number of returned results, with a maximum of 50 per page.

For example:

- https://definethecloudapi-bwh8aehvcgdgg2fq.eastus2-01.azurewebsites.net/api/GetAllDefinitions?pageSize=20 will display 20 results per page.


I've also implemented continuation tokens, which are returned with the data. To access subsequent pages of results, pass the token as the `continuationToken` URL parameter. This will continue the results from where the last page left off. You'll need to implement your own logic to iterate over all the pages, or as many as you require. Once no more results are available, the `continuationToken` will be null.

## Contributing

Contributions, issues and feature requests are welcome. <br />
Feel free to check [issues page](https://github.com/learntocloud/cloud-dictionary/issues) if you want to contribute.<br />

## Author


- Twitter: [@madebygps](https://twitter.com/madebygps)
- Github: [@madebygps](https://github.com/madebygps)

## License

This project is [MIT](https://github.com/learntocloud/cloud-dictionary/blob/main/LICENSE) licensed.

